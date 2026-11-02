using System.Net;
using System.Text;
using System.Text.Json;
using NSubstitute;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.IntegrationTests;

[TestFixture("Development")]
[TestFixture("Production")]
public class ProblemResponseTests(string environment)
{
    private PipelineFactory factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        var users = Substitute.For<IUserRepository>();
        var tasks = Substitute.For<ITaskRepository>();
        users.FindByEmailAsync("EXISTING@EXAMPLE.TEST", Arg.Any<CancellationToken>()).Returns(
            new User(Guid.NewGuid(), "existing@example.test", "EXISTING@EXAMPLE.TEST", "not-used", DateTimeOffset.UtcNow));
        tasks.ListAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<IReadOnlyList<TaskItem>>(new InvalidOperationException("sensitive diagnostic marker")));
        factory = new(environment, users, tasks);
    }

    [OneTimeTearDown]
    public async Task Cleanup() => await factory.DisposeAsync();

    private static IEnumerable<TestCaseData> Cases()
    {
        foreach (var accept in new[] { "application/json", "application/problem+json", "*/*", "text/plain", "text/html", "application/xml" })
            yield return new TestCaseData("validation", accept, HttpStatusCode.BadRequest);
        yield return new("credentials", "text/plain", HttpStatusCode.Unauthorized);
        yield return new("conflict", "application/xml", HttpStatusCode.Conflict);
        yield return new("task-missing", "text/html", HttpStatusCode.NotFound);
        yield return new("unexpected", "text/plain", HttpStatusCode.InternalServerError);
        yield return new("missing-jwt", "application/xml", HttpStatusCode.Unauthorized);
        yield return new("route-missing", "text/html", HttpStatusCode.NotFound);
        yield return new("malformed-json", "application/xml", HttpStatusCode.BadRequest);
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Errors_preserve_status_and_safe_problem_json_for_any_accept(string scenario, string accept, HttpStatusCode expected)
    {
        using var client = factory.Client(scenario is "task-missing" or "unexpected");
        var path = scenario switch
        {
            "validation" or "conflict" or "malformed-json" => "/api/auth/register",
            "credentials" => "/api/auth/login",
            "task-missing" => $"/api/tasks/{Guid.NewGuid()}",
            "route-missing" => "/api/does-not-exist",
            _ => "/api/tasks"
        };
        using var request = new HttpRequestMessage(scenario is "validation" or "credentials" or "conflict" or "malformed-json" ? HttpMethod.Post : HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Accept", accept);
        if (request.Method == HttpMethod.Post)
        {
            var json = scenario switch
            {
                "validation" => "{\"email\":\"bad\",\"password\":\"short\"}",
                "credentials" => "{\"email\":\"missing@example.test\",\"password\":\"Incorrect-password1!\"}",
                "conflict" => "{\"email\":\"existing@example.test\",\"password\":\"Long-test-password1!\"}",
                _ => "{invalid-json"
            };
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(expected));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        using var document = JsonDocument.Parse(body);
        Assert.That(document.RootElement.GetProperty("status").GetInt32(), Is.EqualTo((int)expected));
        Assert.That(document.RootElement.GetProperty("title").GetString(), Is.Not.Empty);
        if (scenario is "validation" or "malformed-json")
            Assert.That(document.RootElement.GetProperty("errors").EnumerateObject().Any(), Is.True);
        Assert.That(body, Does.Not.Contain("sensitive diagnostic marker").And.Not.Contain("StackTrace")
            .And.Not.Contain("TaskManager.Application.AuthService").And.Not.Contain("HEADERS"));
        if (scenario == "missing-jwt")
            Assert.That(response.Headers.WwwAuthenticate.Any(h => h.Scheme == "Bearer"), Is.True);
    }
}

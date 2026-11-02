using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using TaskManager.Application;

namespace TaskManager.IntegrationTests;

public class AuthenticationPipelineTests
{
    private PipelineFactory factory = null!;

    [OneTimeSetUp]
    public void Setup() => factory = new("Testing", Substitute.For<IUserRepository>(), Substitute.For<ITaskRepository>());

    [OneTimeTearDown]
    public async Task Cleanup() => await factory.DisposeAsync();

    [Test]
    public async Task Public_endpoint_is_accessible_and_all_task_operations_require_authentication()
    {
        using var client = factory.Client();
        using var info = await client.GetAsync("/api/public/info");
        Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        foreach (var (method, path) in new[] {
            (HttpMethod.Get, "/api/users/me"), (HttpMethod.Get, "/api/tasks"),
            (HttpMethod.Get, $"/api/tasks/{Guid.NewGuid()}"), (HttpMethod.Post, "/api/tasks"),
            (HttpMethod.Put, $"/api/tasks/{Guid.NewGuid()}"), (HttpMethod.Delete, $"/api/tasks/{Guid.NewGuid()}") })
        {
            using var request = new HttpRequestMessage(method, path);
            using var response = await client.SendAsync(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), $"{method} {path}");
        }
    }

    [TestCase("expired")]
    [TestCase("issuer")]
    [TestCase("audience")]
    [TestCase("key")]
    [TestCase("subject")]
    public async Task Invalid_jwt_is_rejected_without_a_database(string invalid)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, invalid == "subject" ? "invalid-guid" : Guid.NewGuid().ToString()) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            invalid == "key" ? factory.SigningKey + "wrong" : factory.SigningKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(invalid == "issuer" ? "wrong" : "TaskManager",
            invalid == "audience" ? "wrong" : "TaskManager.Web", claims, DateTime.UtcNow.AddHours(-1),
            invalid == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5), credentials);
        using var client = factory.Client();
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        using var response = await client.GetAsync("/api/tasks");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}

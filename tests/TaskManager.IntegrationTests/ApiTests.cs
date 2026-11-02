using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TaskManager.Application;
using TaskManager.Infrastructure;

namespace TaskManager.IntegrationTests;

public class ApiTests
{
    private TestDatabase database = null!;
    private ApiFactory factory = null!;
    private HttpClient anonymous = null!;
    private readonly string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    [OneTimeSetUp]
    public async Task Setup()
    {
        database = await TestDatabase.CreateAsync(); factory = new(database.ConnectionString, key);
        anonymous = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    }
    [OneTimeTearDown]
    public async Task Cleanup()
    {
        anonymous?.Dispose(); if (factory is not null) await factory.DisposeAsync();
        if (database is not null) await database.DisposeAsync();
    }

    [Test]
    public async Task Public_info_is_accessible_and_protected_endpoints_require_jwt()
    {
        Assert.That((await anonymous.GetAsync("/api/public/info")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        foreach (var path in new[] { "/api/users/me", "/api/tasks" })
        {
            using var response = await anonymous.GetAsync(path);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        }
    }

    [Test]
    public async Task Registration_login_me_and_password_storage_work()
    {
        var email = Email(); const string password = "A-long-demo-password1!";
        using var response = await anonymous.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Not.Contain("passwordHash"));
        var user = await new SqlUserRepository(database.Connections).FindByEmailAsync(email.ToUpperInvariant(), default);
        Assert.That(user, Is.Not.Null); Assert.That(user!.PasswordHash, Is.Not.EqualTo(password));
        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var auth = (await login.Content.ReadFromJsonAsync<AuthResult>())!;
        using var client = Authorized(auth.AccessToken);
        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.That(me!.Id, Is.EqualTo(user.Id));
        using var duplicate = await anonymous.PostAsJsonAsync("/api/auth/register", new { email = email.ToUpperInvariant(), password });
        Assert.That(duplicate.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        using var wrong = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = "incorrect-password" });
        Assert.That(wrong.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("not-an-email", "A-long-demo-password1!")]
    [TestCase("valid@example.test", "short")]
    public async Task Invalid_registration_returns_validation_problem(string email, string password)
    {
        using var response = await anonymous.PostAsJsonAsync("/api/auth/register", new { email, password });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.TryGetProperty("errors", out _), Is.True);
    }

    [TestCase("{\"title\":\"\",\"status\":\"Todo\"}")]
    [TestCase("{\"title\":\"Title\",\"status\":\"Unsupported\"}")]
    [TestCase("{\"title\":\"Title\",\"status\":99}")]
    [TestCase("{\"title\":\"Title\",\"status\":1}")]
    [TestCase("{\"title\":null,\"status\":\"Todo\"}")]
    [TestCase("{\"status\":\"Todo\"}")]
    [TestCase("{\"title\":\"Title\",\"status\":null}")]
    [TestCase("null")]
    [TestCase("")]
    [TestCase("{\"title\":\"Title\",\"status\":\"Todo\",\"dueDateUtc\":\"invalid\"}")]
    public async Task Invalid_task_body_returns_400_with_errors(string json)
    {
        using var client = await CreateUserClient();
        using var response = await client.PostAsync("/api/tasks", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("errors"));
    }

    [TestCase(120, 2000, HttpStatusCode.Created)]
    [TestCase(121, 0, HttpStatusCode.BadRequest)]
    [TestCase(1, 2001, HttpStatusCode.BadRequest)]
    public async Task Task_payload_length_boundaries_are_enforced(int titleLength, int descriptionLength, HttpStatusCode expected)
    {
        using var client = await CreateUserClient();
        using var response = await client.PostAsJsonAsync("/api/tasks", new { title = new string('x', titleLength), description = new string('x', descriptionLength), status = "Todo" });
        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }

    [Test]
    public async Task Task_crud_uses_correct_http_semantics_and_isolates_owners()
    {
        using var owner = await CreateUserClient(); using var other = await CreateUserClient();
        var body = new { title = "First task", description = "Overdue is allowed", status = "Todo", dueDateUtc = "2025-01-01T00:00:00Z", userId = Guid.NewGuid() };
        using var create = await owner.PostAsJsonAsync("/api/tasks", body);
        Assert.That(create.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var task = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = task.GetProperty("id").GetGuid(); var path = $"/api/tasks/{id}";
        Assert.That(create.Headers.Location!.ToString(), Does.EndWith(path));
        Assert.That(task.TryGetProperty("userId", out _), Is.False);
        Assert.That((await owner.GetAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await other.GetAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await other.PutAsJsonAsync(path, body)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await other.DeleteAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var otherTasks = await other.GetFromJsonAsync<JsonElement[]>("/api/tasks");
        Assert.That(otherTasks, Is.Empty);
        var update = new { title = "Revised", description = "", status = "Completed", dueDateUtc = (string?)null };
        Assert.That((await owner.PutAsJsonAsync(path, update)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var loaded = await owner.GetFromJsonAsync<JsonElement>(path);
        Assert.That(loaded.GetProperty("status").GetString(), Is.EqualTo("Completed"));
        Assert.That(loaded.GetProperty("dueDateUtc").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That((await owner.DeleteAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await owner.GetAsync(path)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await owner.GetAsync($"/api/tasks/{Guid.NewGuid()}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [TestCase("expired")]
    [TestCase("issuer")]
    [TestCase("audience")]
    [TestCase("key")]
    [TestCase("subject")]
    public async Task Invalid_jwt_is_rejected(string invalid)
    {
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, invalid == "subject" ? "invalid-guid" : Guid.NewGuid().ToString()) };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(invalid == "key" ? key + "wrong" : key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(invalid == "issuer" ? "wrong" : "TaskManager", invalid == "audience" ? "wrong" : "TaskManager.Web", claims,
            DateTime.UtcNow.AddHours(-1), invalid == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5), credentials);
        using var client = Authorized(new JwtSecurityTokenHandler().WriteToken(jwt));
        Assert.That((await client.GetAsync("/api/tasks")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private async Task<HttpClient> CreateUserClient()
    {
        var body = new { email = Email(), password = "A-long-demo-password1!" };
        using var register = await anonymous.PostAsJsonAsync("/api/auth/register", body);
        Assert.That(register.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        using var login = await anonymous.PostAsJsonAsync("/api/auth/login", body);
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = (await login.Content.ReadFromJsonAsync<AuthResult>())!;
        return Authorized(result.AccessToken);
    }
    private HttpClient Authorized(string token)
    {
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return client;
    }
    private static string Email() => $"{Guid.NewGuid():N}@example.test";

    [Test]
    public async Task Unexpected_failure_returns_generic_problem_without_exception_details()
    {
        var failing = Substitute.For<ITaskRepository>();
        failing.ListAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromException<IReadOnlyList<Domain.TaskItem>>(new InvalidOperationException("sensitive diagnostic marker")));
        await using var brokenFactory = new ApiFactory(database.ConnectionString, key, services => services.AddScoped(_ => failing));
        using var client = brokenFactory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var user = new Domain.User(Guid.NewGuid(), "test@example.test", "TEST@EXAMPLE.TEST", "hash", DateTimeOffset.UtcNow);
        var issued = new Api.JwtTokenService(Microsoft.Extensions.Options.Options.Create(new Api.JwtOptions { SigningKey = key }), TimeProvider.System).Issue(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued.AccessToken);
        using var response = await client.GetAsync("/api/tasks");
        var json = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(json, Does.Not.Contain("sensitive diagnostic marker").And.Not.Contain("StackTrace").And.Contain("traceId"));
    }

    private sealed class ApiFactory(string connectionString, string key, Action<IServiceCollection>? configure = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:TaskManager", connectionString);
            builder.UseSetting("Jwt:SigningKey", key);
            builder.UseSetting("Jwt:Issuer", "TaskManager");
            builder.UseSetting("Jwt:Audience", "TaskManager.Web");
            if (configure is not null) builder.ConfigureServices(configure);
        }
    }
}

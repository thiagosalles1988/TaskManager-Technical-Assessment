using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.IntegrationTests;

// Real MVC/JWT/error middleware; only persistence is substituted for pipeline-only tests.
internal sealed class PipelineFactory(string environment, IUserRepository users, ITaskRepository tasks)
    : WebApplicationFactory<Program>
{
    public User Subject { get; } = new(Guid.NewGuid(), "pipeline@example.test", "PIPELINE@EXAMPLE.TEST", "not-used-for-login", DateTimeOffset.UtcNow);
    private readonly string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    internal string SigningKey => key;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("Jwt:SigningKey", key);
        builder.UseSetting("Jwt:Issuer", "TaskManager");
        builder.UseSetting("Jwt:Audience", "TaskManager.Web");
        builder.UseSetting("Database:InitializeOnStart", "false");
        builder.UseSetting("Database:SeedDemo", "false");
        builder.ConfigureServices(services =>
        {
            services.AddScoped(_ => users);
            services.AddScoped(_ => tasks);
        });
    }

    public HttpClient Client(bool authenticated = false)
    {
        var client = CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (authenticated)
        {
            var token = Services.GetRequiredService<ITokenService>().Issue(Subject);
            client.DefaultRequestHeaders.Authorization = new("Bearer", token.AccessToken);
        }
        return client;
    }
}

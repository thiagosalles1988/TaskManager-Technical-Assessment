using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Api;
using TaskManager.Application;
using TaskManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 32 * 1024);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new TaskStateJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new SqlConnectionFactory(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("TaskManager")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:TaskManager.")));
builder.Services.AddScoped<IUserRepository, SqlUserRepository>();
builder.Services.AddScoped<ITaskRepository, SqlTaskRepository>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<DevelopmentSeeder>();
builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt")
    .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32, "JWT signing key must contain at least 32 UTF-8 bytes.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience), "JWT issuer and audience are required.")
    .Validate(o => o.ExpirationMinutes is >= 5 and <= 120, "JWT expiration must be 5 to 120 minutes.")
    .ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, configured) =>
    {
        var jwt = configured.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.FromSeconds(15),
            NameClaimType = "sub"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (!Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var id) || id == Guid.Empty)
                    context.Fail("Invalid subject.");
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async context =>
{
    await ProblemResponses.WriteAsync(context.HttpContext,
        new() { Status = context.HttpContext.Response.StatusCode }, context.HttpContext.RequestAborted);
});
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts(); app.UseHttpsRedirection();
}
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();

var initialize = app.Configuration.GetValue<bool>("Database:InitializeOnStart");
var seed = app.Configuration.GetValue<bool>("Database:SeedDemo");
var migrate = args.Contains("--migrate", StringComparer.Ordinal);
if ((initialize || seed) && !app.Environment.IsDevelopment())
    throw new InvalidOperationException("Automatic initialization and demo seeding require Development environment. Use --migrate for explicit migrations.");
if (initialize || seed || migrate)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);
    if (seed) await scope.ServiceProvider.GetRequiredService<DevelopmentSeeder>().SeedAsync(CancellationToken.None);
}
if (migrate) return;
await app.RunAsync();

public partial class Program { }

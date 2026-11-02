using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.Api;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "TaskManager";
    public string Audience { get; set; } = "TaskManager.Web";
    public string SigningKey { get; set; } = "";
    public int ExpirationMinutes { get; set; } = 30;
}

public sealed class PasswordService : IPasswordService
{
    private readonly object context = new();
    private readonly PasswordHasher<object> hasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    private readonly string dummyHash;
    public PasswordService() => dummyHash = hasher.HashPassword(context, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    public string Hash(string password) => hasher.HashPassword(context, password);
    public bool Verify(string password, string? passwordHash) =>
        hasher.VerifyHashedPassword(context, passwordHash ?? dummyHash, password) != PasswordVerificationResult.Failed;
}

public sealed class JwtTokenService(IOptions<JwtOptions> configured, TimeProvider clock) : ITokenService
{
    public AuthResult Issue(User user)
    {
        var options = configured.Value; var now = clock.GetUtcNow(); var expires = now.AddMinutes(options.ExpirationMinutes);
        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        var signing = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, now.UtcDateTime, expires.UtcDateTime, signing);
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, UserDto.From(user));
    }
}

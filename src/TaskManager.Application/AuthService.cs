using System.Net.Mail;
using TaskManager.Domain;

namespace TaskManager.Application;

public sealed class AuthService(IUserRepository repository, IPasswordService passwords, ITokenService tokens, TimeProvider clock)
{
    public async Task<UserDto> RegisterAsync(Credentials input, CancellationToken cancellationToken)
    {
        var email = input.Email?.Trim() ?? "";
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new DomainValidationException("email", "Enter a valid email address (maximum 254 characters).");
        if (string.IsNullOrWhiteSpace(input.Password) || input.Password.Length is < 12 or > 128)
            throw new DomainValidationException("password", "Password must contain 12 to 128 characters.");
        var normalized = email.ToUpperInvariant();
        if (await repository.FindByEmailAsync(normalized, cancellationToken) is not null) throw new DuplicateEmailException();
        var user = new User(Guid.NewGuid(), email, normalized, passwords.Hash(input.Password), clock.GetUtcNow());
        await repository.AddAsync(user, cancellationToken);
        return UserDto.From(user);
    }

    public async Task<AuthResult> LoginAsync(Credentials input, CancellationToken cancellationToken)
    {
        if (input.Email?.Length > 254 || string.IsNullOrEmpty(input.Password) || input.Password.Length > 128)
            throw new InvalidCredentialsException();
        var user = await repository.FindByEmailAsync((input.Email ?? "").Trim().ToUpperInvariant(), cancellationToken);
        // Verify a dummy hash for unknown accounts to reduce email-enumeration timing differences.
        var verified = passwords.Verify(input.Password, user?.PasswordHash);
        if (user is null || !verified) throw new InvalidCredentialsException();
        return tokens.Issue(user);
    }

    public async Task<UserDto> GetUserAsync(Guid id, CancellationToken cancellationToken) =>
        UserDto.From(await repository.FindByIdAsync(id, cancellationToken) ?? throw new NotFoundException());
}

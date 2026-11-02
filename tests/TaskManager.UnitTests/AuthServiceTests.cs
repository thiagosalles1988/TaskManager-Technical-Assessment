using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.UnitTests;

public class AuthServiceTests
{
    private IUserRepository repository = null!;
    private IPasswordService passwords = null!;
    private ITokenService tokens = null!;
    private AuthService service = null!;
    private readonly Credentials valid = new("  Person@example.test  ", "Long-enough-password1");

    [SetUp]
    public void Setup()
    {
        repository = Substitute.For<IUserRepository>(); passwords = Substitute.For<IPasswordService>();
        tokens = Substitute.For<ITokenService>(); service = new(repository, passwords, tokens, TimeProvider.System);
    }

    [Test]
    public async Task Registers_normalized_email_and_persists_only_password_hash()
    {
        User? saved = null;
        passwords.Hash(valid.Password!).Returns("salted-password-hash");
        repository.AddAsync(Arg.Do<User>(u => saved = u), default).Returns(Task.CompletedTask);
        var result = await service.RegisterAsync(valid, default);
        Assert.That(result.Email, Is.EqualTo("Person@example.test"));
        Assert.That(saved!.NormalizedEmail, Is.EqualTo("PERSON@EXAMPLE.TEST"));
        Assert.That(saved.PasswordHash, Is.EqualTo("salted-password-hash"));
        Assert.That(saved.PasswordHash, Is.Not.EqualTo(valid.Password));
    }

    [Test]
    public void Duplicate_email_is_rejected_case_insensitively()
    {
        repository.FindByEmailAsync("PERSON@EXAMPLE.TEST", default).Returns(Existing());
        Assert.ThrowsAsync<DuplicateEmailException>(() => service.RegisterAsync(valid, default));
    }

    [Test]
    public void Concurrent_duplicate_constraint_is_propagated_as_conflict()
    {
        repository.AddAsync(Arg.Any<User>(), default).ThrowsAsync(new DuplicateEmailException());
        Assert.ThrowsAsync<DuplicateEmailException>(() => service.RegisterAsync(valid, default));
    }

    [TestCase("bad-email", "Long-enough-password1")]
    [TestCase("Name <p@example.test>", "Long-enough-password1")]
    [TestCase(null, "Long-enough-password1")]
    [TestCase("p@example.test", "short")]
    [TestCase("p@example.test", null)]
    public void Invalid_registration_is_rejected(string? email, string? password) =>
        Assert.ThrowsAsync<DomainValidationException>(() => service.RegisterAsync(new(email, password), default));

    [Test]
    public async Task Valid_credentials_issue_token_for_persisted_user()
    {
        var user = Existing(); var expected = new AuthResult("signed-token", DateTimeOffset.UtcNow.AddMinutes(30), UserDto.From(user));
        repository.FindByEmailAsync("PERSON@EXAMPLE.TEST", default).Returns(user);
        passwords.Verify(valid.Password!, user.PasswordHash).Returns(true); tokens.Issue(user).Returns(expected);
        var result = await service.LoginAsync(valid, default);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Wrong_password_is_rejected()
    {
        repository.FindByEmailAsync("PERSON@EXAMPLE.TEST", default).Returns(Existing());
        Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(valid, default));
    }

    [Test]
    public void Missing_user_remains_unauthorized_even_if_password_adapter_returns_true()
    {
        passwords.Verify(valid.Password!, null).Returns(true);
        Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(valid, default));
    }

    private static User Existing() => new(Guid.NewGuid(), "Person@example.test", "PERSON@EXAMPLE.TEST", "saved-hash", DateTimeOffset.UtcNow);
}

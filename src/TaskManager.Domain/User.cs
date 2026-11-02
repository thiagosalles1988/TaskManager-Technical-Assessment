namespace TaskManager.Domain;

// PasswordHash is an internal persistence concept, never an HTTP response contract.
public sealed record User(Guid Id, string Email, string NormalizedEmail, string PasswordHash,
    DateTimeOffset CreatedAtUtc);

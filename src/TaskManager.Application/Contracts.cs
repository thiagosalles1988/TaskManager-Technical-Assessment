using TaskManager.Domain;

namespace TaskManager.Application;

public sealed record TaskInput(string? Title, string? Description, TaskState Status, DateTimeOffset? DueDateUtc);
public sealed record TaskDto(Guid Id, string Title, string Description, TaskState Status,
    DateTimeOffset? DueDateUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc)
{
    public static TaskDto From(TaskItem task) => new(task.Id, task.Title, task.Description, task.Status,
        task.DueDateUtc, task.CreatedAtUtc, task.UpdatedAtUtc);
}
public sealed record Credentials(string? Email, string? Password);
public sealed record UserDto(Guid Id, string Email)
{
    public static UserDto From(User user) => new(user.Id, user.Email);
}
public sealed record AuthResult(string AccessToken, DateTimeOffset ExpiresAtUtc, UserDto User);
public sealed class NotFoundException() : Exception("The requested resource was not found.");
public sealed class InvalidCredentialsException() : Exception("Email or password is incorrect.");
public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string? passwordHash);
}
public interface ITokenService { AuthResult Issue(User user); }

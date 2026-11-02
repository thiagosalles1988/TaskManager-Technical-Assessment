using TaskManager.Domain;

namespace TaskManager.Application;

public sealed class DuplicateEmailException() : Exception("An account with this email already exists.");
public sealed class DuplicateTaskException() : Exception("A task with this identity already exists.");

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
}

public interface ITaskRepository
{
    Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}

namespace TaskManager.Domain;

public enum TaskState { Todo, InProgress, Completed }

public sealed class DomainValidationException(string field, string message) : Exception(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } =
        new Dictionary<string, string[]> { [field] = [message] };
}

public sealed record TaskItem
{
    public Guid Id { get; private init; }
    public Guid UserId { get; private init; }
    public string Title { get; private init; } = "";
    public string Description { get; private init; } = "";
    public TaskState Status { get; private init; }
    public DateTimeOffset? DueDateUtc { get; private init; }
    public DateTimeOffset CreatedAtUtc { get; private init; }
    public DateTimeOffset UpdatedAtUtc { get; private init; }
    private TaskItem() { }

    public static TaskItem Create(Guid id, Guid userId, string? title, string? description,
        TaskState status, DateTimeOffset? dueDateUtc, DateTimeOffset now)
    {
        if (id == Guid.Empty) throw new DomainValidationException("id", "Task identity is required.");
        if (userId == Guid.Empty) throw new DomainValidationException("userId", "Task owner is required.");
        title = title?.Trim();
        if (string.IsNullOrWhiteSpace(title)) throw new DomainValidationException("title", "Title is required.");
        if (title.Length > 120) throw new DomainValidationException("title", "Title must be at most 120 characters.");
        description ??= "";
        if (description.Length > 2000) throw new DomainValidationException("description", "Description must be at most 2000 characters.");
        if (!Enum.IsDefined(status)) throw new DomainValidationException("status", "Choose Todo, InProgress or Completed.");
        return new TaskItem
        {
            Id = id,
            UserId = userId,
            Title = title,
            Description = description,
            Status = status,
            DueDateUtc = dueDateUtc?.ToUniversalTime(),
            CreatedAtUtc = now.ToUniversalTime(),
            UpdatedAtUtc = now.ToUniversalTime()
        };
    }

    public TaskItem Update(string? title, string? description, TaskState status,
        DateTimeOffset? dueDateUtc, DateTimeOffset now) =>
        Create(Id, UserId, title, description, status, dueDateUtc, now) with { CreatedAtUtc = CreatedAtUtc };

    public static TaskItem Restore(Guid id, Guid userId, string title, string description,
        TaskState status, DateTimeOffset? dueDateUtc, DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        Create(id, userId, title, description, status, dueDateUtc, createdAtUtc) with
        { UpdatedAtUtc = updatedAtUtc.ToUniversalTime() };
}

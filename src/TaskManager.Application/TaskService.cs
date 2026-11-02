using TaskManager.Domain;

namespace TaskManager.Application;

public sealed class TaskService(ITaskRepository repository, TimeProvider clock)
{
    public async Task<IReadOnlyList<TaskDto>> ListAsync(Guid owner, CancellationToken cancellationToken) =>
        (await repository.ListAsync(owner, cancellationToken)).Select(TaskDto.From).ToArray();

    public async Task<TaskDto> GetAsync(Guid id, Guid owner, CancellationToken cancellationToken) =>
        TaskDto.From(await repository.FindAsync(id, owner, cancellationToken) ?? throw new NotFoundException());

    public async Task<TaskDto> CreateAsync(Guid owner, TaskInput input, CancellationToken cancellationToken)
    {
        var task = TaskItem.Create(Guid.NewGuid(), owner, input.Title, input.Description,
            input.Status, input.DueDateUtc, clock.GetUtcNow());
        await repository.AddAsync(task, cancellationToken);
        return TaskDto.From(task);
    }

    public async Task UpdateAsync(Guid id, Guid owner, TaskInput input, CancellationToken cancellationToken)
    {
        var task = await repository.FindAsync(id, owner, cancellationToken) ?? throw new NotFoundException();
        var updated = task.Update(input.Title, input.Description, input.Status, input.DueDateUtc, clock.GetUtcNow());
        if (!await repository.UpdateAsync(updated, owner, cancellationToken)) throw new NotFoundException();
    }

    public async Task DeleteAsync(Guid id, Guid owner, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteAsync(id, owner, cancellationToken)) throw new NotFoundException();
    }
}

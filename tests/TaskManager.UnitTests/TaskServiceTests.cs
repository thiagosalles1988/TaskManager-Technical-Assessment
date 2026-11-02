using NSubstitute;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.UnitTests;

public class TaskServiceTests
{
    private ITaskRepository repository = null!;
    private TaskService service = null!;
    private readonly Guid owner = Guid.NewGuid();
    private readonly TaskInput valid = new("Prepare interview", "Architecture", TaskState.Todo, null);

    [SetUp]
    public void Setup() { repository = Substitute.For<ITaskRepository>(); service = new(repository, TimeProvider.System); }

    [Test]
    public async Task Creates_valid_task_with_server_generated_identity_and_owner()
    {
        TaskItem? saved = null;
        repository.AddAsync(Arg.Do<TaskItem>(t => saved = t), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var result = await service.CreateAsync(owner, valid, default);
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(saved!.Id, Is.EqualTo(result.Id));
        Assert.That(saved.UserId, Is.EqualTo(owner));
        Assert.That(result.Title, Is.EqualTo("Prepare interview"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Rejects_missing_title(string? title) => Assert.ThrowsAsync<DomainValidationException>(() =>
        service.CreateAsync(owner, valid with { Title = title }, default));

    [Test]
    public void Rejects_long_title() => Assert.ThrowsAsync<DomainValidationException>(() =>
        service.CreateAsync(owner, valid with { Title = new string('x', 121) }, default));

    [Test]
    public void Rejects_invalid_status() => Assert.ThrowsAsync<DomainValidationException>(() =>
        service.CreateAsync(owner, valid with { Status = (TaskState)3 }, default));

    [Test]
    public async Task Updates_only_task_found_for_authenticated_owner()
    {
        var task = Existing(); TaskItem? saved = null;
        repository.FindAsync(task.Id, owner, default).Returns(task);
        repository.UpdateAsync(Arg.Do<TaskItem>(t => saved = t), owner, default).Returns(true);
        await service.UpdateAsync(task.Id, owner, valid with { Title = "Revised", Status = TaskState.Completed }, default);
        Assert.That(saved!.Title, Is.EqualTo("Revised"));
        Assert.That(saved.Status, Is.EqualTo(TaskState.Completed));
        Assert.That(saved.CreatedAtUtc, Is.EqualTo(task.CreatedAtUtc));
    }

    [Test]
    public async Task Deletes_existing_task() { var id = Guid.NewGuid(); repository.DeleteAsync(id, owner, default).Returns(true); await service.DeleteAsync(id, owner, default); }

    [Test]
    public void Missing_tasks_return_not_found()
    {
        var id = Guid.NewGuid();
        Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(id, owner, default));
        Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(id, owner, valid, default));
        Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(id, owner, default));
    }

    [Test]
    public void Another_user_cannot_read_or_modify_task()
    {
        var task = Existing(); var attacker = Guid.NewGuid();
        repository.FindAsync(task.Id, owner, default).Returns(task);
        Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(task.Id, attacker, default));
        Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(task.Id, attacker, valid, default));
        Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(task.Id, attacker, default));
    }

    [Test]
    public void Deletion_between_read_and_update_returns_not_found()
    {
        var task = Existing(); repository.FindAsync(task.Id, owner, default).Returns(task);
        Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(task.Id, owner, valid, default));
    }

    private TaskItem Existing() => TaskItem.Create(Guid.NewGuid(), owner, "Before", "", TaskState.Todo, null, DateTimeOffset.UtcNow.AddDays(-1));
}

using TaskManager.Domain;

namespace TaskManager.UnitTests;

public class DomainTests
{
    private readonly Guid owner = Guid.NewGuid();
    private readonly DateTimeOffset now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Creation_trims_title_defaults_description_and_accepts_overdue_date()
    {
        var task = TaskItem.Create(Guid.NewGuid(), owner, "  Prepare interview  ", null,
            TaskState.Todo, now.AddDays(-1).ToOffset(TimeSpan.FromHours(-3)), now);
        Assert.Multiple(() =>
        {
            Assert.That(task.Title, Is.EqualTo("Prepare interview"));
            Assert.That(task.Description, Is.Empty);
            Assert.That(task.UserId, Is.EqualTo(owner));
            Assert.That(task.DueDateUtc!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(task.DueDateUtc, Is.EqualTo(now.AddDays(-1)));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Missing_title_is_rejected(string? title) =>
        Assert.Throws<DomainValidationException>(() => Create(title));

    [TestCase(120, true)]
    [TestCase(121, false)]
    public void Title_length_boundary_is_enforced(int length, bool valid)
    {
        if (valid) Assert.DoesNotThrow(() => Create(new string('x', length)));
        else Assert.Throws<DomainValidationException>(() => Create(new string('x', length)));
    }

    [TestCase(2000, true)]
    [TestCase(2001, false)]
    public void Description_length_boundary_is_enforced(int length, bool valid)
    {
        if (valid) Assert.DoesNotThrow(() => Create("Title", new string('x', length)));
        else Assert.Throws<DomainValidationException>(() => Create("Title", new string('x', length)));
    }

    [Test]
    public void Unsupported_status_is_rejected() => Assert.Throws<DomainValidationException>(() =>
        TaskItem.Create(Guid.NewGuid(), owner, "Title", "", (TaskState)90, null, now));

    [Test]
    public void Empty_owner_is_rejected() => Assert.Throws<DomainValidationException>(() =>
        TaskItem.Create(Guid.NewGuid(), Guid.Empty, "Title", "", TaskState.Todo, null, now));

    [Test]
    public void Update_preserves_identity_creation_and_changes_editable_fields()
    {
        var original = Create("Before");
        var updated = original.Update("After", "Details", TaskState.Completed, null, now.AddMinutes(1));
        Assert.Multiple(() =>
        {
            Assert.That(updated.Title, Is.EqualTo("After"));
            Assert.That(updated.Status, Is.EqualTo(TaskState.Completed));
            Assert.That(updated.Id, Is.EqualTo(original.Id));
            Assert.That(updated.UserId, Is.EqualTo(owner));
            Assert.That(updated.CreatedAtUtc, Is.EqualTo(now));
            Assert.That(updated.UpdatedAtUtc, Is.EqualTo(now.AddMinutes(1)));
        });
    }

    private TaskItem Create(string? title, string? description = "") =>
        TaskItem.Create(Guid.NewGuid(), owner, title, description, TaskState.Todo, null, now);
}

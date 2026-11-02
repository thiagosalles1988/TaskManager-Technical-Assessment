using Microsoft.Data.SqlClient;
using TaskManager.Application;
using TaskManager.Domain;
using TaskManager.Infrastructure;

namespace TaskManager.IntegrationTests;

public class RepositoryTests
{
    private TestDatabase database = null!;
    private SqlUserRepository users = null!;
    private SqlTaskRepository tasks = null!;
    private User owner = null!;
    private User other = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        database = await TestDatabase.CreateAsync();
        users = new(database.Connections);
        tasks = new(database.Connections);
        owner = NewUser(); other = NewUser();
        await users.AddAsync(owner, default); await users.AddAsync(other, default);
    }

    [OneTimeTearDown]
    public async Task Cleanup() { if (database is not null) await database.DisposeAsync(); }

    [Test]
    public async Task User_round_trip_preserves_hash_and_enforces_normalized_email_uniqueness()
    {
        var loaded = await users.FindByEmailAsync(owner.NormalizedEmail, default);
        Assert.That(loaded, Is.EqualTo(owner));
        Assert.ThrowsAsync<DuplicateEmailException>(() => users.AddAsync(owner with { Id = Guid.NewGuid() }, default));
    }

    [Test]
    public async Task Task_crud_round_trip_and_ownership_are_enforced_in_sql()
    {
        var now = DateTimeOffset.UtcNow;
        var task = TaskItem.Create(Guid.NewGuid(), owner.Id, "O'Brien; DROP TABLE Users;--", "<script>alert(1)</script>",
            TaskState.Todo, now.AddDays(-2), now);
        await tasks.AddAsync(task, default);
        Assert.That(await tasks.FindAsync(task.Id, owner.Id, default), Is.EqualTo(task));
        Assert.That(await tasks.FindAsync(task.Id, other.Id, default), Is.Null);
        Assert.That((await tasks.ListAsync(other.Id, default)).Any(t => t.Id == task.Id), Is.False);
        var updated = task.Update("Updated", "Details", TaskState.InProgress, null, now.AddMinutes(1));
        Assert.That(await tasks.UpdateAsync(updated, other.Id, default), Is.False);
        var forged = TaskItem.Create(task.Id, other.Id, "Forged", "", TaskState.Completed, null, now);
        Assert.That(await tasks.UpdateAsync(forged, other.Id, default), Is.False);
        Assert.That(await tasks.FindAsync(task.Id, owner.Id, default), Is.EqualTo(task));
        Assert.That(await tasks.DeleteAsync(task.Id, other.Id, default), Is.False);
        Assert.That(await tasks.UpdateAsync(updated, owner.Id, default), Is.True);
        Assert.That(await tasks.FindAsync(task.Id, owner.Id, default), Is.EqualTo(updated));
        Assert.That(await tasks.DeleteAsync(task.Id, owner.Id, default), Is.True);
        Assert.That(await tasks.FindAsync(task.Id, owner.Id, default), Is.Null);
        Assert.That(await tasks.UpdateAsync(updated, owner.Id, default), Is.False);
        Assert.That(await tasks.DeleteAsync(task.Id, owner.Id, default), Is.False);
    }

    [TestCase("", 0, "CK_Tasks_Title")]
    [TestCase("Valid title", 7, "CK_Tasks_Status")]
    public async Task Independent_check_constraints_reject_invalid_data(string title, byte status, string constraint)
    {
        await using var connection = await database.Connections.OpenAsync(default);
        await using var command = new SqlCommand("INSERT dbo.Tasks (Id,UserId,Title,Description,Status,CreatedAtUtc,UpdatedAtUtc) VALUES (NEWID(),@Owner,@Title,'',@Status,SYSUTCDATETIME(),SYSUTCDATETIME())", connection);
        command.Parameters.Add(new SqlParameter("@Owner", System.Data.SqlDbType.UniqueIdentifier) { Value = owner.Id });
        command.Parameters.Add(new SqlParameter("@Title", System.Data.SqlDbType.NVarChar, 120) { Value = title });
        command.Parameters.Add(new SqlParameter("@Status", System.Data.SqlDbType.TinyInt) { Value = status });
        var error = Assert.ThrowsAsync<SqlException>(async () => await command.ExecuteNonQueryAsync());
        Assert.That(error!.Number, Is.EqualTo(547));
        Assert.That(error.Message, Does.Contain(constraint));
    }

    [Test]
    public async Task Schema_initialization_does_not_reapply_versions()
    {
        await using var connection = await database.Connections.OpenAsync(default);
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.SchemaVersions", connection);
        var before = (int)(await command.ExecuteScalarAsync())!;
        await new DatabaseInitializer(database.Connections).InitializeAsync(default);
        Assert.That((int)(await command.ExecuteScalarAsync())!, Is.EqualTo(before));
        Assert.That(before, Is.GreaterThan(0));
    }

    [Test]
    public void Foreign_key_rejects_missing_owner()
    {
        var task = TaskItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Orphan", "", TaskState.Todo, null, DateTimeOffset.UtcNow);
        var error = Assert.ThrowsAsync<SqlException>(() => tasks.AddAsync(task, default));
        Assert.That(error!.Number, Is.EqualTo(547));
        Assert.That(error.Message, Does.Contain("FK_Tasks_Users"));
    }

    private static User NewUser()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        return new(Guid.NewGuid(), email, email.ToUpperInvariant(), "a-persisted-hash", DateTimeOffset.UtcNow);
    }
}

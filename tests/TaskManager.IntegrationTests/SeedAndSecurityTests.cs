using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TaskManager.Api;
using TaskManager.Application;

namespace TaskManager.IntegrationTests;

public class SeedAndSecurityTests
{
    [Test]
    public void Password_hash_is_salted_and_verifies_only_the_correct_password()
    {
        var service = new PasswordService();
        const string password = "a-long-development-password!";
        var first = service.Hash(password); var second = service.Hash(password);
        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(service.Verify(password, first), Is.True);
        Assert.That(service.Verify("wrong", first), Is.False);
        Assert.That(service.Verify(password, null), Is.False);
    }

    [Test]
    public async Task Development_seed_is_repeatable_and_preserves_existing_edits()
    {
        await using var db = await TestDatabase.CreateAsync();
        var users = new Infrastructure.SqlUserRepository(db.Connections);
        var tasks = new Infrastructure.SqlTaskRepository(db.Connections);
        var passwords = new PasswordService();
        var tokens = NSubstitute.Substitute.For<ITokenService>();
        var auth = new AuthService(users, passwords, tokens, TimeProvider.System);
        var seeder = new DevelopmentSeeder(new TestEnvironment("Development"), auth, users, tasks, TimeProvider.System);
        await seeder.SeedAsync(default);
        var user = (await users.FindByEmailAsync(DevelopmentSeeder.Email.ToUpperInvariant(), default))!;
        var originalHash = user.PasswordHash;
        var list = await tasks.ListAsync(user.Id, default);
        Assert.That(list, Has.Count.EqualTo(3));
        var edit = list[0].Update("Keep my edit", "", Domain.TaskState.Completed, null, DateTimeOffset.UtcNow);
        await tasks.UpdateAsync(edit, user.Id, default);
        await seeder.SeedAsync(default);
        Assert.That((await tasks.ListAsync(user.Id, default)), Has.Count.EqualTo(3));
        Assert.That((await tasks.FindAsync(edit.Id, user.Id, default))!.Title, Is.EqualTo("Keep my edit"));
        Assert.That((await users.FindByIdAsync(user.Id, default))!.PasswordHash, Is.EqualTo(originalHash));
        Assert.That(passwords.Verify(DevelopmentSeeder.Password, originalHash), Is.True);
        var production = new DevelopmentSeeder(new TestEnvironment("Production"), auth, users, tasks, TimeProvider.System);
        Assert.ThrowsAsync<InvalidOperationException>(() => production.SeedAsync(default));
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Test]
    public async Task Concurrent_seed_starts_recover_from_duplicate_sample_insert()
    {
        await using var db = await TestDatabase.CreateAsync();
        var users = new Infrastructure.SqlUserRepository(db.Connections);
        var inner = new Infrastructure.SqlTaskRepository(db.Connections);
        var tasks = new SimultaneousFirstLookup(inner);
        var auth = new AuthService(users, new PasswordService(), NSubstitute.Substitute.For<ITokenService>(), TimeProvider.System);
        var user = await auth.RegisterAsync(new(DevelopmentSeeder.Email, DevelopmentSeeder.Password), default);
        var seeder = new DevelopmentSeeder(new TestEnvironment("Development"), auth, users, tasks, TimeProvider.System);
        await Task.WhenAll(seeder.SeedAsync(default), seeder.SeedAsync(default));
        Assert.That(await inner.ListAsync(user.Id, default), Has.Count.EqualTo(3));
    }

    // Delays two genuine SQL reads until both see the same missing task, making the insert race deterministic.
    private sealed class SimultaneousFirstLookup(ITaskRepository inner) : ITaskRepository
    {
        private int arrivals;
        private readonly TaskCompletionSource bothRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Domain.TaskItem?> FindAsync(Guid id, Guid owner, CancellationToken cancellationToken)
        {
            var result = await inner.FindAsync(id, owner, cancellationToken);
            var count = Interlocked.Increment(ref arrivals);
            if (count == 2) bothRead.SetResult();
            if (count <= 2) await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            return result;
        }
        public Task<IReadOnlyList<Domain.TaskItem>> ListAsync(Guid owner, CancellationToken cancellationToken) => inner.ListAsync(owner, cancellationToken);
        public Task AddAsync(Domain.TaskItem task, CancellationToken cancellationToken) => inner.AddAsync(task, cancellationToken);
        public Task<bool> UpdateAsync(Domain.TaskItem task, Guid owner, CancellationToken cancellationToken) => inner.UpdateAsync(task, owner, cancellationToken);
        public Task<bool> DeleteAsync(Guid id, Guid owner, CancellationToken cancellationToken) => inner.DeleteAsync(id, owner, cancellationToken);
    }
}

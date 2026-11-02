using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.Api;

public sealed class DevelopmentSeeder(IHostEnvironment environment, AuthService auth, IUserRepository users, ITaskRepository tasks, TimeProvider clock)
{
    public const string Email = "demo@taskmanager.local";
    // Public, disposable assessment credentials, only used by explicitly enabled development seeding.
    public const string Password = "Demo-TaskManager-2026!";
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Demo seeding requires Development.");
        var user = await users.FindByEmailAsync(Email.ToUpperInvariant(), cancellationToken);
        if (user is null)
        {
            try { await auth.RegisterAsync(new(Email, Password), cancellationToken); }
            catch (DuplicateEmailException) { /* Another development host may have created the account. */ }
            user = await users.FindByEmailAsync(Email.ToUpperInvariant(), cancellationToken) ?? throw new InvalidOperationException("Demo account creation failed.");
        }
        var samples = new[]
        {
            (new Guid("cbd224c1-3897-4a44-8d7a-4b2a6ca90001"), "Prepare the interview demo", "Walk through authentication, task CRUD and ownership isolation.", TaskState.Todo, 2),
            (new Guid("cbd224c1-3897-4a44-8d7a-4b2a6ca90002"), "Review architecture decisions", "Explain why each layer exists and where business rules live.", TaskState.InProgress, -1),
            (new Guid("cbd224c1-3897-4a44-8d7a-4b2a6ca90003"), "Run the automated checks", "Verify backend tests and the Angular production build.", TaskState.Completed, 0)
        };
        foreach (var (id, title, description, status, days) in samples)
        {
            if (await tasks.FindAsync(id, user.Id, cancellationToken) is not null) continue;
            var now = clock.GetUtcNow();
            try { await tasks.AddAsync(TaskItem.Create(id, user.Id, title, description, status, now.AddDays(days), now), cancellationToken); }
            catch (DuplicateTaskException)
            {
                // Only accept a concurrently inserted sample belonging to this same demo owner.
                if (await tasks.FindAsync(id, user.Id, cancellationToken) is null) throw;
            }
        }
    }
}

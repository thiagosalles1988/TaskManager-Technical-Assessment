using Microsoft.Data.SqlClient;
using TaskManager.Infrastructure;

namespace TaskManager.IntegrationTests;

// All creation/deletion is restricted to a generated name. No application database is reused.
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly string adminConnection;
    private readonly string name = "TaskManagerTest_" + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; }
    public SqlConnectionFactory Connections => new(ConnectionString);

    private TestDatabase(string configured)
    {
        var builder = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master", Pooling = false };
        adminConnection = builder.ConnectionString;
        builder.InitialCatalog = name;
        ConnectionString = builder.ConnectionString;
    }

    public static async Task<TestDatabase> CreateAsync()
    {
        var configured = Environment.GetEnvironmentVariable("TaskManagerTests__ConnectionString");
        if (string.IsNullOrWhiteSpace(configured))
            Assert.Ignore("Set TaskManagerTests__ConnectionString to a SQL Server connection permitted to create isolated databases. No SQL behavior was verified.");
        var db = new TestDatabase(configured!);
        try
        {
            await using var connection = new SqlConnection(db.adminConnection);
            await connection.OpenAsync();
            await using var command = new SqlCommand($"CREATE DATABASE [{db.name}]", connection);
            await command.ExecuteNonQueryAsync();
            await new DatabaseInitializer(db.Connections).InitializeAsync(CancellationToken.None);
            return db;
        }
        catch { await db.DisposeAsync(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        if (!name.StartsWith("TaskManagerTest_", StringComparison.Ordinal) || name.Length != 48)
            throw new InvalidOperationException("Unsafe test database name.");
        await using var connection = new SqlConnection(adminConnection);
        await connection.OpenAsync();
        // Only a generated GUID identifier is interpolated; never configuration/user input.
        await using var command = new SqlCommand($"IF DB_ID(N'{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]; END", connection);
        await command.ExecuteNonQueryAsync();
    }
}

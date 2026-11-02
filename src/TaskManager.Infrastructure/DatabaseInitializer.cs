using System.Data;
using Microsoft.Data.SqlClient;

namespace TaskManager.Infrastructure;

public sealed class DatabaseInitializer(SqlConnectionFactory connections)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using (var command = new SqlCommand("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'TaskManager.Schema',
                @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
            IF @result < 0 THROW 51000, 'Cannot acquire schema initialization lock.', 1;
            IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
                CREATE TABLE dbo.SchemaVersions (Version nvarchar(200) NOT NULL PRIMARY KEY, AppliedAtUtc datetime2(7) NOT NULL);
            """, connection, transaction))
            await command.ExecuteNonQueryAsync(cancellationToken);
        var assembly = typeof(DatabaseInitializer).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql", StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            await using var check = new SqlCommand("SELECT COUNT(*) FROM dbo.SchemaVersions WHERE Version=@Version", connection, transaction);
            check.Parameters.Add(new SqlParameter("@Version", SqlDbType.NVarChar, 200) { Value = resource });
            if (Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken)) != 0) continue;
            await using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException("Missing SQL resource.");
            using var reader = new StreamReader(stream);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            await using var migration = new SqlCommand(sql, connection, transaction);
            await migration.ExecuteNonQueryAsync(cancellationToken);
            await using var record = new SqlCommand("INSERT dbo.SchemaVersions(Version,AppliedAtUtc) VALUES(@Version,SYSUTCDATETIME())", connection, transaction);
            record.Parameters.Add(new SqlParameter("@Version", SqlDbType.NVarChar, 200) { Value = resource });
            await record.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}

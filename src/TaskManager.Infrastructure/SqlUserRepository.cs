using TaskManager.Application;
using TaskManager.Domain;
using System.Data;
using Microsoft.Data.SqlClient;

namespace TaskManager.Infrastructure;

public sealed class SqlUserRepository(SqlConnectionFactory connections) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        FindAsync("SELECT Id,Email,NormalizedEmail,PasswordHash,CreatedAtUtc FROM dbo.Users WHERE NormalizedEmail=@Value",
            new SqlParameter("@Value", SqlDbType.NVarChar, 254) { Value = normalizedEmail }, cancellationToken);
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        FindAsync("SELECT Id,Email,NormalizedEmail,PasswordHash,CreatedAtUtc FROM dbo.Users WHERE Id=@Value",
            new SqlParameter("@Value", SqlDbType.UniqueIdentifier) { Value = id }, cancellationToken);

    private async Task<User?> FindAsync(string sql, SqlParameter parameter, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(parameter);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new User(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc)));
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            INSERT dbo.Users(Id,Email,NormalizedEmail,PasswordHash,CreatedAtUtc)
            VALUES (@Id,@Email,@NormalizedEmail,@PasswordHash,@CreatedAtUtc)
            """, connection);
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = user.Id });
        command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 254) { Value = user.Email });
        command.Parameters.Add(new SqlParameter("@NormalizedEmail", SqlDbType.NVarChar, 254) { Value = user.NormalizedEmail });
        command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.NVarChar, 512) { Value = user.PasswordHash });
        command.Parameters.Add(new SqlParameter("@CreatedAtUtc", SqlDbType.DateTime2) { Value = user.CreatedAtUtc.UtcDateTime });
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { throw new DuplicateEmailException(); }
    }
}

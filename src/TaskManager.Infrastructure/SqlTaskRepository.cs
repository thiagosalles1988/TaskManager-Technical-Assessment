using TaskManager.Application;
using TaskManager.Domain;
using System.Data;
using Microsoft.Data.SqlClient;

namespace TaskManager.Infrastructure;

public sealed class SqlTaskRepository(SqlConnectionFactory connections) : ITaskRepository
{
    private const string Columns = "Id,UserId,Title,Description,Status,DueDateUtc,CreatedAtUtc,UpdatedAtUtc";
    public async Task<IReadOnlyList<TaskItem>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand($"SELECT {Columns} FROM dbo.Tasks WHERE UserId=@UserId ORDER BY CreatedAtUtc DESC, Id", connection);
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }
    public async Task<TaskItem?> FindAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand($"SELECT {Columns} FROM dbo.Tasks WHERE Id=@Id AND UserId=@UserId", connection);
        AddIdentity(command, id, userId);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            INSERT dbo.Tasks(Id,UserId,Title,Description,Status,DueDateUtc,CreatedAtUtc,UpdatedAtUtc)
            VALUES(@Id,@UserId,@Title,@Description,@Status,@DueDateUtc,@CreatedAtUtc,@UpdatedAtUtc)
            """, connection);
        AddIdentity(command, task.Id, task.UserId);
        AddFields(command, task);
        command.Parameters.Add(new SqlParameter("@CreatedAtUtc", SqlDbType.DateTime2) { Value = task.CreatedAtUtc.UtcDateTime });
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        { throw new DuplicateTaskException(); }
    }
    public async Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken)
    {
        if (task.UserId != userId) return false;
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            UPDATE dbo.Tasks SET Title=@Title,Description=@Description,Status=@Status,
                DueDateUtc=@DueDateUtc,UpdatedAtUtc=@UpdatedAtUtc
            WHERE Id=@Id AND UserId=@UserId
            """, connection);
        AddIdentity(command, task.Id, userId);
        AddFields(command, task);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var command = new SqlCommand("DELETE dbo.Tasks WHERE Id=@Id AND UserId=@UserId", connection);
        AddIdentity(command, id, userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
    private static void AddIdentity(SqlCommand command, Guid id, Guid userId)
    {
        command.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.UniqueIdentifier) { Value = userId });
    }
    private static void AddFields(SqlCommand command, TaskItem task)
    {
        command.Parameters.Add(new SqlParameter("@Title", SqlDbType.NVarChar, 120) { Value = task.Title });
        command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar, 2000) { Value = task.Description });
        command.Parameters.Add(new SqlParameter("@Status", SqlDbType.TinyInt) { Value = (byte)task.Status });
        command.Parameters.Add(new SqlParameter("@DueDateUtc", SqlDbType.DateTime2) { Value = (object?)task.DueDateUtc?.UtcDateTime ?? DBNull.Value });
        command.Parameters.Add(new SqlParameter("@UpdatedAtUtc", SqlDbType.DateTime2) { Value = task.UpdatedAtUtc.UtcDateTime });
    }
    private static TaskItem Map(SqlDataReader reader) => TaskItem.Restore(reader.GetGuid(0), reader.GetGuid(1),
        reader.GetString(2), reader.GetString(3), (TaskState)reader.GetByte(4),
        reader.IsDBNull(5) ? null : Utc(reader, 5), Utc(reader, 6), Utc(reader, 7));
    private static DateTimeOffset Utc(SqlDataReader reader, int ordinal) =>
        new(DateTime.SpecifyKind(reader.GetDateTime(ordinal), DateTimeKind.Utc));
}

using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.Domain;

namespace TaskManager.Api;

public sealed class TaskStateJsonConverter : JsonConverter<TaskState>
{
    public override TaskState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? reader.GetString() switch
        {
            "Todo" => TaskState.Todo,
            "InProgress" => TaskState.InProgress,
            "Completed" => TaskState.Completed,
            _ => throw new JsonException("Status must be Todo, InProgress or Completed.")
        } : throw new JsonException("Status must be a supported string name.");

    public override void Write(Utf8JsonWriter writer, TaskState value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value switch
        {
            TaskState.Todo => "Todo",
            TaskState.InProgress => "InProgress",
            TaskState.Completed => "Completed",
            _ => throw new JsonException("Unsupported task status.")
        });
}

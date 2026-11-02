using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TaskManager.Api;

// SQL datetime2(7) and .NET preserve 100 ns ticks. Never infer an offset from server locale.
public sealed partial class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitTimestamp();

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String ||
            !ExplicitTimestamp().IsMatch(reader.GetString()!) || !reader.TryGetDateTimeOffset(out var value))
            throw new JsonException("Use an ISO 8601 timestamp with seconds, Z or an explicit offset, and at most seven fractional digits.");
        return value.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime);
}

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.IntegrationTests;

public class JsonContractTests
{
    private PipelineFactory factory = null!;
    private JsonSerializerOptions json = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        factory = new("Testing", Substitute.For<IUserRepository>(), Substitute.For<ITaskRepository>());
        json = factory.Services.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;
    }

    [OneTimeTearDown]
    public async Task Cleanup() => await factory.DisposeAsync();

    [TestCase("\"2026-11-04T15:00:00\"")]
    [TestCase("\"2026-11-04\"")]
    [TestCase("\"2026-11-04T15:00:00.12345678Z\"")]
    [TestCase("\"2026-02-30T12:00:00Z\"")]
    [TestCase("\"not-a-date\"")]
    [TestCase("\"2026-11-04T15:00:00+15:00\"")]
    [TestCase("123")]
    public void Due_dates_require_valid_offset_and_at_most_seven_fraction_digits(string value) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TaskInput>(
            "{\"title\":\"Date contract\",\"status\":\"Todo\",\"dueDateUtc\":" + value + "}", json));

    [TestCase("2026-11-04T15:00:00.1234567Z")]
    [TestCase("2026-11-04T20:30:00.1234567+05:30")]
    [TestCase("2026-11-04T12:00:00.1234567-03:00")]
    public void Explicit_offsets_are_normalized_without_losing_ticks(string input)
    {
        var result = JsonSerializer.Deserialize<DateTimeOffset>("\"" + input + "\"", json);
        var expected = new DateTimeOffset(2026, 11, 4, 15, 0, 0, TimeSpan.Zero).AddTicks(1_234_567);
        Assert.That(result, Is.EqualTo(expected));
        Assert.That(result.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Responses_write_canonical_utc_with_full_precision()
    {
        var value = new DateTimeOffset(2026, 11, 4, 17, 0, 0, TimeSpan.FromHours(2)).AddTicks(1_234_567);
        Assert.That(JsonSerializer.Serialize(value, json), Is.EqualTo("\"2026-11-04T15:00:00.1234567Z\""));
    }

    [Test]
    public void Due_date_remains_optional_and_nullable()
    {
        Assert.That(JsonSerializer.Deserialize<TaskInput>("{\"title\":\"Optional\"}", json)!.DueDateUtc, Is.Null);
        Assert.That(JsonSerializer.Deserialize<TaskInput>("{\"title\":\"Optional\",\"dueDateUtc\":null}", json)!.DueDateUtc, Is.Null);
    }

    [TestCase("\"Todo, InProgress\"")]
    [TestCase("\"0\"")]
    [TestCase("1")]
    [TestCase("null")]
    [TestCase("\"todo\"")]
    [TestCase("\"Unsupported\"")]
    public void Status_accepts_only_a_single_documented_name(string value) =>
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TaskState>(value, json));

    [TestCase("Todo", TaskState.Todo)]
    [TestCase("InProgress", TaskState.InProgress)]
    [TestCase("Completed", TaskState.Completed)]
    public void Supported_status_names_round_trip(string name, TaskState expected)
    {
        Assert.That(JsonSerializer.Deserialize<TaskState>("\"" + name + "\"", json), Is.EqualTo(expected));
        Assert.That(JsonSerializer.Serialize(expected, json), Is.EqualTo("\"" + name + "\""));
    }
}

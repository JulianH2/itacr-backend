using System.Text.Json;

using Itacr.Api.Common.Http;

namespace Itacr.Api.Tests.Common;

/// <summary>
/// Every stored time is UTC (ITACR-66): a datetime2 read from SQL Server has no kind, and a browser would read it as its local time.
/// </summary>
public sealed class UtcDateTimeJsonConverterTests
{
    private static readonly UtcDateTimeJsonConverter Converter = new();

    private static JsonSerializerOptions Options() => new() { Converters = { Converter } };

    [Fact]
    public void Write_ShouldAnswerWithTheUtcTime_WhenTheTimeHasNoKind()
    {
        // Arrange
        var time = new DateTime(2026, 10, 16, 18, 0, 0, DateTimeKind.Unspecified);

        // Act
        var json = JsonSerializer.Serialize(time, Options());

        // Assert
        Assert.Equal("\"2026-10-16T18:00:00Z\"", json);
    }

    [Fact]
    public void Write_ShouldAnswerWithTheUtcTime_WhenTheTimeIsLocal()
    {
        // Arrange
        var time = new DateTime(2026, 10, 16, 18, 0, 0, DateTimeKind.Local);

        // Act
        var json = JsonSerializer.Serialize(time, Options());

        // Assert
        Assert.EndsWith("Z\"", json);
        Assert.Equal(time.ToUniversalTime(), JsonSerializer.Deserialize<DateTime>(json, Options()));
    }

    [Fact]
    public void Write_ShouldKeepTheUtcTime_WhenItIsAlreadyUtc()
    {
        // Arrange
        var time = new DateTime(2026, 10, 16, 18, 0, 0, DateTimeKind.Utc);

        // Act
        var json = JsonSerializer.Serialize(time, Options());

        // Assert
        Assert.Equal("\"2026-10-16T18:00:00Z\"", json);
    }

    [Fact]
    public void Read_ShouldAnswerWithTheUtcTime_WhenTheJsonHasNoZone()
    {
        // Arrange
        var json = "\"2026-10-16T18:00:00\"";

        // Act
        var time = JsonSerializer.Deserialize<DateTime>(json, Options());

        // Assert
        Assert.Equal(DateTimeKind.Utc, time.Kind);
        Assert.Equal(new DateTime(2026, 10, 16, 18, 0, 0, DateTimeKind.Utc), time);
    }
}

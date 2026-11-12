using System.Text.Json;
using System.Text.Json.Serialization;

namespace Itacr.Api.Common.Http;

/// <summary>
/// Every stored time is UTC, but a datetime2 read from SQL Server has no kind and would be written without a zone, which a browser
/// reads as its local time (the order detail showed the times six hours ahead in Mexico). Every DateTime is answered with its "Z".
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        AsUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(AsUtc(value));

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}

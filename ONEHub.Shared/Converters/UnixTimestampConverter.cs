using System.Text.Json;
using System.Text.Json.Serialization;

namespace ONEHub.Shared.Converters;

public class UnixTimestampConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return 0;
            }

            // Try to parse as long directly
            if (long.TryParse(stringValue, out var longValue))
            {
                return longValue;
            }

            // Try to parse as DateTime and convert to Unix timestamp
            if (DateTimeOffset.TryParse(stringValue, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dateTime))
            {
                return dateTime.ToUnixTimeSeconds();
            }

            throw new JsonException($"Unable to convert string '{stringValue}' to Unix timestamp");
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

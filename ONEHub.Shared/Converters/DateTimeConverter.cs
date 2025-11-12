using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ONEHub.Shared.Converters;

public class DateTimeConverter : JsonConverter<DateTimeOffset>
{
    private readonly string[] _dateTimeFormats = {
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd",
        "MM/dd/yyyy HH:mm:ss",
        "MM/dd/yyyy",
        "dd/MM/yyyy HH:mm:ss",
        "dd/MM/yyyy"
    };

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var dateTimeString = reader.GetString();
            if (string.IsNullOrEmpty(dateTimeString))
            {
                return DateTimeOffset.MinValue;
            }

            // Try parsing with different formats
            foreach (var format in _dateTimeFormats)
            {
                if (DateTimeOffset.TryParseExact(dateTimeString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }

            // If none of the specific formats work, try general parsing
            if (DateTimeOffset.TryParse(dateTimeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var generalResult))
            {
                return generalResult;
            }

            // If all parsing fails, try parsing as Unix timestamp
            if (long.TryParse(dateTimeString, out var unixTimestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            }

            throw new JsonException($"Unable to parse DateTimeOffset from: {dateTimeString}");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var unixTimestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
    }
}


using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Satori.Kimai.Converters;

internal class KimaiDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = new KimaiNullableDateTimeOffsetConverter();
        var value = converter.Read(ref reader, typeToConvert, options);

        return value ?? throw new FormatException("The DateTimeOffset value cannot be null");
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var converter = new KimaiNullableDateTimeOffsetConverter();
        converter.Write(writer, value, options);
    }
}

internal partial class KimaiNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (KimaiDateTimeOffsetPattern().IsMatch(value))
        {
            value = value.Insert(value.Length - 2, ":");
        }
        if (DateTimeOffset.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    [GeneratedRegex(@"^\d{4}-$\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{4}$")]
    private static partial Regex KimaiDateTimeOffsetPattern();
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Satori.Converters;

public class IntAsStringConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new InvalidOperationException("The value in the JSON was null.  An integer was expected.");
        return int.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
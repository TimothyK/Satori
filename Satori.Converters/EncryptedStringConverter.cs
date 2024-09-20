using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Satori.Converters;

public class EncryptedStringConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var encryptedValue = reader.GetString();
        if (encryptedValue == null)
        {
            return string.Empty;
        }

        try
        {
            return DecryptString(encryptedValue);
        }
        catch (FormatException)
        {
            return encryptedValue;
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        var encryptedValue = EncryptString(value);
        writer.WriteStringValue(encryptedValue);
    }

    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="plainText"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// Yes, I know this isn't encryption, it is just very weak obfuscation.
    /// That's fine for now because Blazor doesn't really support encryption yet,
    /// and the client could easily decode the data anyway.
    /// I'm just obfuscating it a little bit to make it harder to casually read.
    /// </para>
    /// </remarks>
    private static string EncryptString(string plainText)
    {
        var buffer = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(buffer);
    }

    private static string DecryptString(string cipherText)
    {
        var buffer = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(buffer);
    }
}
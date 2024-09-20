using Satori.Converters;
using System.Text.Json.Serialization;

namespace Satori.Kimai;

public class ConnectionSettings
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Base URL to the Kimai server.  The path should be empty.
    /// </summary>
    public required Uri Url { get; init; }

    public KimaiAuthenticationMethod AuthenticationMethod { get; init; } = KimaiAuthenticationMethod.Token;

    /// <summary>
    /// Token used to authenticate with to the Kimai server
    /// </summary>
    /// <remarks>Only used if <see cref="AuthenticationMethod"/> is <see cref="KimaiAuthenticationMethod.Token"/> </remarks>
    [JsonConverter(typeof(EncryptedStringConverter))]
    public string? ApiToken { get; init; }

    /// <summary>
    /// The user's Name field
    /// </summary>
    /// <remarks>Only used if <see cref="AuthenticationMethod"/> is <see cref="KimaiAuthenticationMethod.Password"/> </remarks>
    public string? UserName { get; init; }
    /// <summary>
    /// API token that was set for the user
    /// </summary>
    /// <remarks>Only used if <see cref="AuthenticationMethod"/> is <see cref="KimaiAuthenticationMethod.Password"/> </remarks>
    [JsonConverter(typeof(EncryptedStringConverter))]
    public string? ApiPassword { get; init; }

    public static readonly ConnectionSettings Default = new()
    {
        Enabled = false,
        Url = new Uri("https://kimai.test"),
        UserName = string.Empty,
        ApiPassword = string.Empty,
    };
}

public enum KimaiAuthenticationMethod
{
    Token,
    Password,
}
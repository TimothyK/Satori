namespace Satori.Kimai;

public class ConnectionSettings
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Base URL to the Kimai server.  The path should be empty.
    /// </summary>
    public required Uri Url { get; init; }
    /// <summary>
    /// The user's Name field
    /// </summary>
    public required string UserName { get; init; }
    /// <summary>
    /// API token that was set for the user
    /// </summary>
    public required string ApiPassword { get; init; }

    public static readonly ConnectionSettings Default = new()
    {
        Enabled = false,
        Url = new Uri("https://kimai.test"),
        UserName = string.Empty,
        ApiPassword = string.Empty,
    };
}
namespace Satori.AzureDevOps;

public class ConnectionSettings
{
    public bool Enabled { get; init; } = true;
    public required Uri Url { get; init; }
    public required string PersonalAccessToken { get; init; }

    public static ConnectionSettings Default = new()
    {
        Enabled = false,
        Url = new Uri("https://devops.test/Org"),
        PersonalAccessToken = string.Empty,
    };
}
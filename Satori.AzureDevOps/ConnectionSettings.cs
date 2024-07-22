namespace Satori.AzureDevOps;

public class ConnectionSettings
{
    public bool Enabled { get; init; } = true;
    public required Uri Url { get; init; }
    public required string PersonalAccessToken { get; init; }
}
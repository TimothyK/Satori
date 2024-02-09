namespace Satori.AzureDevOps;

public class ConnectionSettings
{
    public required Uri Url { get; init; }
    public required string PersonalAccessToken { get; init; }
}
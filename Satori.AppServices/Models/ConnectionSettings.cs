namespace Satori.AppServices.Models;

public class ConnectionSettings
{
    public required AzureDevOps.ConnectionSettings AzureDevOps { get; init; }
    public required Kimai.ConnectionSettings Kimai { get; init; }
    public required MessageQueues.ConnectionSettings MessageQueue { get; init; }
}
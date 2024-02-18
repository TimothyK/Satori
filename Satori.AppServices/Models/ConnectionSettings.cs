namespace Satori.AppServices.Models;

public class ConnectionSettings
{
    public required AzureDevOps.ConnectionSettings AzureDevOps { get; init; }
}
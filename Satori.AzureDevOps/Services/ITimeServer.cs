namespace Satori.AzureDevOps.Services;

public interface ITimeServer
{
    DateTimeOffset GetUtcNow();
}
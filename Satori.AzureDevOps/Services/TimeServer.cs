namespace Satori.AzureDevOps.Services;

public class TimeServer : ITimeServer
{
    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
using Satori.TimeServices;

namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;

internal class TestTimeServer : ITimeServer
{
    private DateTimeOffset? _currentTime;

    public void SetTime(DateTimeOffset time)
    {
        _currentTime = time;
    }

    public DateTimeOffset GetUtcNow()
    {
        return _currentTime ?? DateTimeOffset.UtcNow;
    }
}
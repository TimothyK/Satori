using System;

namespace Satori.TimeServices;

public class TestTimeServer : ITimeServer
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
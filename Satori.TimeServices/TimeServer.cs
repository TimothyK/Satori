using System;

namespace Satori.TimeServices
{
    public class TimeServer : ITimeServer
    {
        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
    }
}
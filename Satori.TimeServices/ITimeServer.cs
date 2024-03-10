using System;

namespace Satori.TimeServices
{
    public interface ITimeServer
    {
        DateTimeOffset GetUtcNow();
    }
}
using Satori.TimeServices;

namespace Satori.AppServices.Models;

public class Cache<T>(ITimeServer timeServer)
{
    public DateTimeOffset LastUpdateTime { get; private set; } = DateTimeOffset.MinValue;
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(1);

    private T? _value;
    public T? Value
    {
        get => _value;
        set
        {
            _value = value;
            LastUpdateTime = timeServer.GetUtcNow();
        }
    }

    public bool IsExpired => LastUpdateTime + MaxAge < timeServer.GetUtcNow();

    /// <summary>
    /// This Semaphore can be used when updating the cache
    /// </summary>
    public readonly SemaphoreSlim Semaphore = new(1, 1);
}

public enum CachingAlgorithm
{
    UseCache,
    ForceRefresh
}
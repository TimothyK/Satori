using System;
using System.Threading;
using System.Threading.Tasks;

namespace Satori.TimeServices;

public class Cache<T>(Func<Task<T>> fetchAsync, ITimeServer timeServer)
{
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(1);
    public bool IsExpired => LastUpdateTime + MaxAge <= timeServer.GetUtcNow();


    public DateTimeOffset LastUpdateTime { get; private set; } = DateTimeOffset.MinValue;
    private T Value { get; set; }

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<T> GetValueAsync(CachingAlgorithm cachingAlgorithm = CachingAlgorithm.UseCache)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (cachingAlgorithm == CachingAlgorithm.UseCache && !IsExpired)
            {
                return Value!;
            }

            Value = await fetchAsync();
            LastUpdateTime = timeServer.GetUtcNow();
            return Value;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public enum CachingAlgorithm
{
    UseCache,
    ForceRefresh
}
using Shouldly;

namespace Satori.TimeServices.Tests.Caching;

[TestClass]
public class CacheTests
{
    private readonly TestTimeServer _timeServer = new();
    private readonly Cache<int> _cache; // object under test

    public CacheTests()
    {
        _timeServer.SetTime(DateTimeOffset.Now);
        _cache = new Cache<int>(FetchValueAsync, _timeServer);
    }

    #region Helpers

    #region Arrange

    private readonly ManualResetEventSlim _fetchSignal = new(true);

    #endregion Arrange

    #region Act

    private Task<int> FetchValueAsync()
    {
        _fetchSignal.Wait(TimeSpan.FromMilliseconds(100));
        Interlocked.Increment(ref _fetchCallCount);
        return Task.FromResult(1);
    }

    #endregion Act

    #region Assert

    private int _fetchCallCount;
    
    #endregion Assert

    #endregion Helpers

    [TestMethod]
    public void CacheInitialization()
    {
        //Assert
        _cache.IsExpired.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SetValue_SetsLastUpdateTime()
    {
        //Arrange
        var t = _timeServer.GetUtcNow();

        //Act
        _ = await _cache.GetValueAsync();

        //Assert
        _cache.LastUpdateTime.ShouldBe(t);
    }

    [TestMethod]
    public async Task OneSecond_NotExpired()
    {
        //Arrange
        var t = _timeServer.GetUtcNow();
        _ = await _cache.GetValueAsync();

        //Act
        _timeServer.SetTime(t + _cache.MaxAge - TimeSpan.FromTicks(1));

        //Assert
        _cache.IsExpired.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task OneMinute_Expired()
    {
        //Arrange
        var t = _timeServer.GetUtcNow();
        _ = await _cache.GetValueAsync();

        //Act
        _timeServer.SetTime(t + _cache.MaxAge);

        //Assert
        _cache.IsExpired.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ConcurrentThreads_OnlyOneFetch()
    {
        // Arrange
        _fetchSignal.Reset();

        // Act
        var task1 = Task.Run(() => _cache.GetValueAsync());
        var task2 = Task.Run(() => _cache.GetValueAsync());
        
        _fetchSignal.Set();

        await Task.WhenAll(task1, task2);

        // Assert
        _fetchCallCount.ShouldBe(1);

    }
    
    [TestMethod]
    public async Task CacheExpiresBetweenCalls_TwoFetches()
    {
        // Arrange
        var t = _timeServer.GetUtcNow();

        // Act
        _ = await _cache.GetValueAsync();
        _timeServer.SetTime(t + _cache.MaxAge);
        _ = await _cache.GetValueAsync();

        // Assert
        _fetchCallCount.ShouldBe(2);

    }
}
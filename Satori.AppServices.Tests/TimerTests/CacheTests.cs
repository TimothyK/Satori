using Satori.AppServices.Models;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Shouldly;

namespace Satori.AppServices.Tests.TimerTests;

[TestClass]
public class CacheTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
    private readonly TestTimeServer _timeServer = new();
    private readonly Cache<int> _cache; //object under test

    public CacheTests()
    {
        _timeServer.SetTime(DateTimeOffset.Now);
        _cache = new Cache<int>(_timeServer);
    }

    [TestMethod]
    public void CacheInitialization()
    {
        //Assert
        _cache.IsExpired.ShouldBeTrue();
    }

    [TestMethod]
    public void SetValue_SetsLastUpdateTime()
    {
        //Arrange
        var t = _timeServer.GetUtcNow();

        //Act
        _cache.Value = 1;

        //Assert
        _cache.LastUpdateTime.ShouldBe(t);
    }

    [TestMethod]
    public void OneSecond_NotExpired()
    {
        //Arrange
        var t = _timeServer.GetUtcNow();
        _cache.Value = 1;

        //Act
        _timeServer.SetTime(t + OneSecond);

        //Assert
        _cache.IsExpired.ShouldBeFalse();
    }
    
    [TestMethod]
    public void OneMinute_Expired()
    {
        //Arrange
        var t = _timeServer.GetUtcNow();
        _cache.Value = 1;
        _cache.MaxAge.ShouldBe(OneMinute);

        //Act
        _timeServer.SetTime(t + OneMinute);

        //Assert
        _cache.IsExpired.ShouldBeFalse();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.Kimai.Models;
using Satori.TimeServices;
using Shouldly;

namespace Satori.AppServices.Tests.TimerTests;

[TestClass]
public class GetActivelyTimedWorkItemIdsTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestKimaiServer _kimai;

    public GetActivelyTimedWorkItemIdsTests()
    {
        var serviceCollection = new SatoriServiceCollection();
        serviceCollection.AddSingleton<UserService>();
        serviceCollection.AddScoped<TimerService>();  // Scoped will reset the cache on GetActivelyTimesWorkItems for each test
        _serviceProvider = serviceCollection.BuildServiceProvider();

        _kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();
    }

    #region Helpers

    #region Arrange

    private TimeEntry BuildTimeEntry()
    {
        var lastEntry = _kimai.GetLastEntry();
        if (lastEntry != null && lastEntry.End == null)
        {
            var duration = RandomGenerator.TimeSpan(TimeSpan.FromMinutes(30)).ToNearest(TimeSpan.FromMinutes(3));
            lastEntry.End = lastEntry.Begin + duration;
        }
        var startTime = lastEntry?.End ?? DateTimeOffset.Now.AddHours(-27).TruncateSeconds();

        var entry = Builder.Builder<TimeEntry>.New().Build(t =>
        {
            t.Id = Sequence.TimeEntryId.Next();
            t.User = _kimai.CurrentUser;
            t.Begin = startTime;
            t.End = null;
            t.Activity = lastEntry?.Activity ?? BuildActivity();
        }, int.MaxValue);
        entry.Project = entry.Activity.Project ?? BuildProject();
        
        _kimai.AddTimeEntry(entry);

        return entry;
    }

    private static Activity BuildActivity()
    {
        return Builder.Builder<Activity>.New().Build(a => a.Id = Sequence.ActivityId.Next(), int.MaxValue);
    }
    
    private static Project BuildProject()
    {
        return Builder.Builder<Project>.New().Build(p => p.Id = Sequence.ProjectId.Next(), int.MaxValue);
    }

    
    private static User BuildUser()
    {
        return Builder.Builder<User>.New().Build(user =>
        {
            user.Id = Sequence.KimaiUserId.Next();
            user.Enabled = true;
            user.Language = "en_CA";
        });
    }

    #endregion Arrange

    #region Act

    private async Task<IReadOnlyCollection<int>> GetActivelyTimedWorkItemIds()
    {
        var srv = _serviceProvider.GetRequiredService<TimerService>();
        return await srv.GetActivelyTimedWorkItemIdsAsync();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Act
        var workItemIds = await GetActivelyTimedWorkItemIds();

        //Assert
        workItemIds.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task CurrentUserTiming()
    {
        //Arrange
        var entry = BuildTimeEntry();
        entry.End = null;
        entry.Description = "D#12345";

        //Act
        var workItemIds = await GetActivelyTimedWorkItemIds();

        //Assert
        workItemIds.Count.ShouldBe(1);
        workItemIds.ShouldContain(12345);
    }
    
    [TestMethod]
    public async Task AllTimersStopped()
    {
        //Arrange
        var entry = BuildTimeEntry();
        entry.End = entry.Begin + TimeSpan.FromMinutes(30);
        entry.Description = "D#12345";

        //Act
        var workItemIds = await GetActivelyTimedWorkItemIds();

        //Assert
        workItemIds.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task BugAndTask_ReturnsBothIds()
    {
        //Arrange
        var entry = BuildTimeEntry();
        entry.End = null;
        entry.Description = "D#12345 App Crashes » D#12346 Testing";

        //Act
        var workItemIds = await GetActivelyTimedWorkItemIds();

        //Assert
        workItemIds.Count.ShouldBe(2);
        workItemIds.ShouldContain(12345);
        workItemIds.ShouldContain(12346);
    }
    
    [TestMethod]
    public async Task DifferentUsers()
    {
        //Arrange
        var entry1 = BuildTimeEntry();
        var entry2 = BuildTimeEntry();

        entry1.End = null;
        entry1.Description = "D#12345 App Crashes";

        entry2.End = null;
        entry2.User = BuildUser();
        entry2.Description = "D#12348 App Hangs";

        //Act
        var workItemIds = await GetActivelyTimedWorkItemIds();

        //Assert
        workItemIds.Count.ShouldBe(2);
        workItemIds.ShouldContain(12345);
        workItemIds.ShouldContain(12348);
    }

    [TestMethod]
    public async Task IsCached()
    {
        // Arrange
        var timeServer = _serviceProvider.GetRequiredService<TestTimeServer>();
        var t = DateTimeOffset.Now;
        timeServer.SetTime(t);

        // Act 1
        var workItemIds1 = await GetActivelyTimedWorkItemIds();
        workItemIds1.ShouldBeEmpty();

        // Add time entry
        var entry = BuildTimeEntry();
        entry.End = null;
        entry.Description = "D#12345";

        // Act 2
        var workItemIds2 = await GetActivelyTimedWorkItemIds();
        workItemIds2.ShouldBeEmpty();  // Still using the cached value

        // Act 3
        timeServer.SetTime(t + TimeSpan.FromMinutes(1));
        var workItemIds3 = await GetActivelyTimedWorkItemIds();

        // Assert
        workItemIds3.Count.ShouldBe(1);
        workItemIds3.ShouldContain(12345);
    }
}
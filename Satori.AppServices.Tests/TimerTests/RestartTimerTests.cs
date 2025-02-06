using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels;
using Satori.Kimai.Models;
using Shouldly;

namespace Satori.AppServices.Tests.TimerTests;

[TestClass]
public class RestartTimerTests
{

    protected readonly TestAlertService AlertService = new();
    private protected TestKimaiServer Kimai { get; } = new();
    private protected TestAzureDevOpsServer AzureDevOps { get; } = new();

    public RestartTimerTests()
    {
        Person.Me = null;  //Clear cache

    }

    #region Helpers

    #region Arrange

    private TimeEntry BuildTimeEntry()
    {
        var lastEntry = Kimai.GetLastEntry();
        if (lastEntry != null && lastEntry.End == null)
        {
            var duration = RandomGenerator.TimeSpan(TimeSpan.FromMinutes(30)).ToNearest(TimeSpan.FromMinutes(3));
            lastEntry.End = lastEntry.Begin + duration;
        }
        var startTime = lastEntry?.End ?? DateTimeOffset.Now.AddHours(-27).TruncateSeconds();

        var entry = Builder.Builder<TimeEntry>.New().Build(t =>
        {
            t.Id = Sequence.TimeEntryId.Next();
            t.User = Kimai.CurrentUser;
            t.Begin = startTime;
            t.End = null;
            t.Activity = lastEntry?.Activity ?? BuildActivity();
        }, int.MaxValue);
        entry.Project = entry.Activity.Project;
        
        Kimai.AddTimeEntry(entry);

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

    #endregion Arrange

    #region Act

    private async Task<TimeEntry> RestartTimerAsync(params int[] entryIds)
    {   
        var userService = new UserService(AzureDevOps.AsInterface(), Kimai.AsInterface(), AlertService);
        var timerServer = new TimerService(Kimai.AsInterface(), userService, AlertService);

        //Act
        await timerServer.RestartTimerAsync(entryIds);

        //Assert
        var newEntry = Kimai.GetLastEntry();
        newEntry.ShouldNotBeNull();
        return newEntry;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var entry = BuildTimeEntry();

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        actual.Id.ShouldBeGreaterThan(entry.Id);
        actual.Project.Id.ShouldBe(entry.Project.Id);
        actual.Activity.Id.ShouldBe(entry.Activity.Id);
        actual.User.Id.ShouldBe(entry.User.Id);
        actual.End.ShouldBeNull();
    }

    #region Begin

    [TestMethod]
    public async Task Begin_NoRunningTask_SetToNow()
    {
        //Arrange
        var entry = BuildTimeEntry();
        var startTime = DateTimeOffset.Now.TruncateSeconds();

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        var endTime = DateTimeOffset.Now;
        actual.Begin.ShouldBeGreaterThanOrEqualTo(startTime);
        actual.Begin.ShouldBeLessThanOrEqualTo(endTime);
    }
    
    [TestMethod]
    public async Task Begin_RunningTask_RunningTaskIsStopped()
    {
        //Arrange
        var entry = BuildTimeEntry();
        var runningEntry = BuildTimeEntry();
        entry.End.ShouldNotBeNull();
        runningEntry.End.ShouldBeNull();

        var timeServer = new TestTimeServer();
        var stopTime = DateTimeOffset.UtcNow.AddMinutes(3).TruncateSeconds();
        timeServer.SetTime(stopTime);
        Kimai.TimeServer = timeServer;

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        runningEntry.End.ShouldNotBeNull();
        runningEntry.End.Value.ShouldBe(stopTime);

        actual.Begin.ShouldBe(runningEntry.End.Value);
    }
    
    [TestMethod]
    public async Task Begin_MultipleRunningTasks_MyRunningTaskIsStopped()
    {
        //Arrange
        var entry = BuildTimeEntry();
        var myRunningEntry = BuildTimeEntry();
        var colleagueRunningEntry = BuildTimeEntry().With(t => t.User = KimaiUserBuilder.BuildUser());
        myRunningEntry.End = null;

        var timeServer = new TestTimeServer();
        var stopTime = DateTimeOffset.UtcNow.AddMinutes(3).TruncateSeconds();
        timeServer.SetTime(stopTime);
        Kimai.TimeServer = timeServer;

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        myRunningEntry.End.ShouldNotBeNull();
        myRunningEntry.End.Value.ShouldBe(stopTime);
        
        colleagueRunningEntry.End.ShouldBeNull();

        actual.Begin.ShouldBe(myRunningEntry.End.Value);
    }

    #endregion Begin

    #region User

    [TestMethod]
    public async Task User_RestartColleagueTime_CreatesMyTime()
    {
        //Arrange
        var entry = BuildTimeEntry().With(t => t.User = KimaiUserBuilder.BuildUser());

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        actual.User.ShouldBe(Kimai.CurrentUser);
    }

    #endregion User

    #region Project and Activity

    [TestMethod]
    public async Task Activity_MultipleTimeEntriesForDifferentActivities_ThrowsInvalidOp()
    {
        //Arrange
        var entry1 = BuildTimeEntry();
        var entry2 = BuildTimeEntry().With(t => t.Activity = BuildActivity());

        //Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => RestartTimerAsync(entry1.Id, entry2.Id));

        //Assert
        ex.Message.ShouldBe("The activities found were not unique");
    }
    
    [TestMethod]
    public async Task Project_MultipleTimeEntriesForDifferentProjects_ThrowsInvalidOp()
    {
        //Arrange
        var entry1 = BuildTimeEntry();
        var entry2 = BuildTimeEntry();
        entry1.Activity.Project = null;
        entry2.Project = BuildProject();

        //Act
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => RestartTimerAsync(entry1.Id, entry2.Id));

        //Assert
        ex.Message.ShouldBe("The projects found were not unique");
    }

    #endregion Project and Activity

}
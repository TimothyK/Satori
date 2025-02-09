using CodeMonkeyProjectiles.Linq;
using Moq;
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
        entry.Project = entry.Activity.Project ?? BuildProject();
        
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

    #region Assert

    [TestCleanup]
    public void TearDown()
    {
        AlertService.VerifyNoMessagesWereBroadcast();
    }

    #endregion Assert

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
        await RestartTimerAsync(entry1.Id, entry2.Id);

        //Assert
        AlertService.LastException.ShouldNotBeNull();
        AlertService.LastException.Message.ShouldBe("The activities found were not unique");
        AlertService.DisableVerifications();
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
        await RestartTimerAsync(entry1.Id, entry2.Id);

        //Assert
        AlertService.LastException.ShouldNotBeNull();
        AlertService.LastException.Message.ShouldBe("The projects found were not unique");
        AlertService.DisableVerifications();
    }

    #endregion Project and Activity

    #region Comments

    [TestMethod]
    public async Task Comment()
    {
        //Arrange
        const string description = "Meetings";
        var entry = BuildTimeEntry();
        entry.Description = description;

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        actual.Description.ShouldBe(description);
    }
    
    [TestMethod]
    public async Task WorkItemComment()
    {
        //Arrange
        const string description = "D#12345 Create Widget > D#12346 Coding";
        var entry = BuildTimeEntry();
        entry.Description = description;

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        actual.Description.ShouldBe(description);
    }
    
    [TestMethod]
    public async Task Accomplishments_NotCopied()
    {
        //Arrange
        const string description = """
                                   🏆Implemented wire frame
                                   D#12345 Create Widget > D#12346 Coding
                                   """;
        var entry = BuildTimeEntry();
        entry.Description = description;

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        actual.Description.ShouldBe("D#12345 Create Widget > D#12346 Coding");
    }
    
    [TestMethod]
    public async Task ScrumCommentTypes_NotCopied()
    {
        //Arrange
        const string description = """
                                   🏆Drank Coffee
                                   D#12345 Create Widget > D#12346 Coding
                                   🧱Bathroom queues
                                   Meetings
                                   🧠Bladder Control
                                   """;
        var entry = BuildTimeEntry();
        entry.Description = description;

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        actual.Description.ShouldBe("""
                                    D#12345 Create Widget > D#12346 Coding
                                    Meetings
                                    """);
    }
    
    [TestMethod]
    public async Task DuplicateComments_Removed()
    {
        //Arrange
        const string description = """
                                   D#12345 Create Widget > D#12346 Coding
                                   Meetings
                                   """;
        var entry1 = BuildTimeEntry();
        entry1.Description = description;
        var entry2 = BuildTimeEntry();
        entry2.Description = description;

        //Act
        var actual = await RestartTimerAsync(entry1.Id, entry2.Id);

        //Assert
        actual.Description.ShouldBe("""
                                    D#12345 Create Widget > D#12346 Coding
                                    Meetings
                                    """);
    }

    #endregion Comments

    #region Error Handling

    [TestMethod]
    public async Task ConnectionError_BroadcastError()
    {
        //Arrange
        Kimai.Mock
            .Setup(srv => srv.GetTimeSheetAsync(It.IsAny<TimeSheetFilter>()))
            .Throws<ApplicationException>();

        var entry = BuildTimeEntry();


        //Act
        await RestartTimerAsync(entry.Id);

        //Assert
        AlertService.LastException.ShouldNotBeNull();
        AlertService.LastException.ShouldBeOfType<ApplicationException>();
        AlertService.DisableVerifications();
    }

    #endregion Error Handling
}
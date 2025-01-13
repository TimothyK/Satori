using Builder;
using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai.Models;
using Shouldly;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;

namespace Satori.AppServices.Tests.TimerTests;

[TestClass]
public class TimerTests
{
    #region Helpers

    #region Arrange

    private protected TestKimaiServer Kimai { get; } = new() {CurrentUser = DefaultUser};

    protected static readonly User DefaultUser = Builder<User>.New().Build(user =>
    {
        user.Id = Sequence.KimaiUserId.Next();
        user.Enabled = true;
        user.Language = "en_CA";
    });

    private TimeEntry BuildTimeEntry()
    {
        var kimaiEntry = BuildKimaiTimeEntry();

        var activity = BuildActivity(kimaiEntry.Activity);

        var entry = new TimeEntry
        {
            Id = kimaiEntry.Id,
            Begin = kimaiEntry.Begin,
            End = kimaiEntry.End,
            ParentActivitySummary = activity,
        };

        activity.TimeEntries = entry.Yield().ToArray();
        
        var taskSummary = BuildTaskSummary(activity);
        taskSummary.TimeEntries = entry.Yield().ToArray();

        activity.TaskSummaries = taskSummary.Yield().ToArray();

        return entry;
    }

    private KimaiTimeEntry BuildKimaiTimeEntry()
    {
        var project = new Project
        {
            Customer = new Customer
            {
                Name = "Code Monkey Projectiles"
            },
            Name = "123 Skunk Works"
        };

        var entry = new KimaiTimeEntry
        {
            User = DefaultUser,
            Activity = new Activity
            {
                Project = project,
                Name = "Project Overhead",
            },
            Project = project,
            Id = Sequence.TimeEntryId.Next(),
            Begin = DateTimeOffset.Now,
            End = null,
        };

        Kimai.AddTimeEntry(entry);
        return entry;
    }

    private TaskSummary BuildTaskSummary(ActivitySummary activity)
    {
        return new TaskSummary
        {
            ParentActivitySummary = activity,
            TimeEntries = []
        };
    }

    private ActivitySummary BuildActivity(Activity kimaiActivity)
    {
        var project = BuildProject(kimaiActivity.Project);
        var activity = new ActivitySummary
        {
            ActivityName = kimaiActivity.Name,
            ParentProjectSummary = project,
            TimeEntries = [],
            Url = Kimai.BaseUrl,
            TaskSummaries = []
        };
        project.Activities = activity.Yield().ToArray();

        return activity;
    }

    private ProjectSummary BuildProject(Project kimaiProject)
    {
        var daySummary = BuildDaySummary();
        var project = new ProjectSummary
        {
            ProjectName = kimaiProject.Name,
            ParentDay = daySummary,
            CustomerName = kimaiProject.Customer.Name,
            Activities = [],
            Url = Kimai.BaseUrl
        };

        daySummary.Projects = project.Yield().ToArray();

        return project;
    }

    private DaySummary BuildDaySummary()
    {
        return new DaySummary
        {
            ParentPeriod = BuildPeriodSummary(),
            Projects = [],
            Url = Kimai.BaseUrl
        };
    }

    private PeriodSummary BuildPeriodSummary()
    {
        return new PeriodSummary();
    }

    #endregion Arrange

    #region Act

    private async Task StopTimerAsync(TimeEntry timeEntry)
    {
        var testTimeServer = new TimerService(Kimai.AsInterface());

        await testTimeServer.StopTimerAsync(timeEntry);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest_Stop()
    {
        //Arrange
        var timeEntry = BuildTimeEntry();
        var kimaiEntry = Kimai.GetLastEntry() ?? throw new InvalidOperationException();

        //Act
        await StopTimerAsync(timeEntry);

        //Assert
        kimaiEntry.End.ShouldNotBeNull();
    }
}
using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Extensions;
using Satori.AppServices.Tests.Extensions;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.Models;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class TimeEntryDailyStandUpTests : DailyStandUpTests
{
    public TimeEntryDailyStandUpTests()
    {
        DefaultActivity = TestActivities.SingleRandom();
    }

    #region Helpers

    #region Arrange

    private Activity DefaultActivity { get; }

    private KimaiTimeEntry BuildTimeEntry() => BuildTimeEntry(DefaultActivity);

    #endregion Arrange

    #region Act

    private async Task<TimeEntry[]> GetTimesAsync()
    {
        var today = Today;
        var days = await GetStandUpDaysAsync(today.AddDays(-6), today);

        return days.SelectMany(day => day.Projects.SelectMany(project => project.Activities.SelectMany(activity => activity.TimeEntries)))
            .ToArray();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Length.ShouldBe(1);
        var entry = entries.Single();
        entry.Id.ShouldBe(kimaiEntry.Id);
    }
    
    [TestMethod]
    public async Task ParentTaskSummary()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.ParentTaskSummary.ShouldNotBeNull();
        entry.ParentTaskSummary.TimeEntries.ShouldContain(entry);
        entry.ParentTaskSummary.ParentActivitySummary.ActivityId.ShouldBe(kimaiEntry.Activity.Id);
    }
    
    [TestMethod]
    public async Task ParentActivity()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.ParentActivitySummary.ActivityId.ShouldBe(kimaiEntry.Activity.Id);
    }
    
    [TestMethod]
    public async Task ParentProject()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.ParentActivitySummary.ParentProjectSummary.ProjectId.ShouldBe(kimaiEntry.Activity.Project.Id);
    }
    
    [TestMethod]
    public async Task ParentDay()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.ParentActivitySummary.ParentProjectSummary.ParentDay.Date.ShouldBe(DateOnly.FromDateTime(kimaiEntry.Begin.Date));
    }

    #region Time

    [TestMethod]
    public async Task Begin()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().Begin.ShouldBe(kimaiEntry.Begin);
    }
    
    [TestMethod]
    public async Task End()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().End.ShouldBe(kimaiEntry.End);
    }
    
    [TestMethod]
    public async Task IsRunning()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End.ShouldNotBeNull();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().IsRunning.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task TotalTime()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End.ShouldNotBeNull();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().TotalTime.ShouldBe(kimaiEntry.End.Value - kimaiEntry.Begin);
    }

    [TestMethod]
    public async Task OrderedChronologically()
    {
        //Arrange
        var today = Today;
        var kimaiEntry1 = BuildTimeEntry();
        var kimaiEntry2 = BuildTimeEntry();
        var kimaiEntry3 = BuildTimeEntry();
        kimaiEntry1.Begin.ShouldBeLessThan(kimaiEntry3.Begin);
        kimaiEntry3.End.ShouldNotBeNull();
        kimaiEntry2.Begin = kimaiEntry3.End.Value.AddMinutes(5).TruncateSeconds();
        kimaiEntry2.End = kimaiEntry2.Begin.Add(TimeSpan.FromMinutes(30));

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        var entries = days.Single().Projects.Single().Activities.Single().TimeEntries;
        entries[0].Id.ShouldBe(kimaiEntry2.Id);
        entries[1].Id.ShouldBe(kimaiEntry3.Id);
        entries[2].Id.ShouldBe(kimaiEntry1.Id);
    }
    
    #endregion Time

    #region Running Task

    [TestMethod]
    public async Task RunningTask_NullEndTime()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End = null;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.End.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task RunningTask_TotalTime()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End = null;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.TotalTime.ShouldBe(TimeSpan.Zero);
    }
    
    [TestMethod]
    public async Task RunningTask_IsRunning()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End = null;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.IsRunning.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task RunningTask_ParentsAreRunning()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End = null;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.ParentTaskSummary.ShouldNotBeNull();
        entry.ParentTaskSummary.IsRunning.ShouldBeTrue();
        entry.ParentTaskSummary.ParentActivitySummary.IsRunning.ShouldBeTrue();
        entry.ParentTaskSummary.ParentActivitySummary.ParentProjectSummary.IsRunning.ShouldBeTrue();
        entry.ParentTaskSummary.ParentActivitySummary.ParentProjectSummary.ParentDay.IsRunning.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task RunningTask_CannotExport()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.End = null;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.CanExport.ShouldBeFalse();
    }

    #endregion Running Task

    #region Export

    [TestMethod]
    public async Task Exported()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Exported = RandomGenerator.Boolean();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().Exported.ShouldBe(kimaiEntry.Exported);
    }
    
    [TestMethod]
    public async Task CanExport_NotExported_True()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task CanExport_AlreadyExported_False()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Exported = true;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_ActivityToBeDetermined_False()
    {
        //Arrange
        var activity = DefaultActivity.Copy()
            .With(a => a.Id = Sequence.ActivityId.Next())
            .With(a => a.Name = "TBD");
        var kimaiEntry = BuildTimeEntry(activity, Today);
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_ProjectToBeDetermined_False()
    {
        //Arrange
        var project = DefaultActivity.Project.Copy()
            .With(p => p.Id = Sequence.ProjectId.Next())
            .With(p => p.Name = "TBD");
        var activity = DefaultActivity.Copy()
            .With(a => a.Id = Sequence.ActivityId.Next())
            .With(a => a.Project = project);
        var kimaiEntry = BuildTimeEntry(activity, Today);
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_ActivityDeactivated_False()
    {
        //Arrange
        var activity = DefaultActivity.Copy()
            .With(a => a.Id = Sequence.ActivityId.Next())
            .With(a => a.Visible = false);
        var kimaiEntry = BuildTimeEntry(activity, Today);
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_ProjectDeactivated_False()
    {
        //Arrange
        var project = DefaultActivity.Project.Copy()
            .With(p => p.Id = Sequence.ProjectId.Next())
            .With(p => p.Visible = false);
        var activity = DefaultActivity.Copy()
            .With(a => a.Id = Sequence.ActivityId.Next())
            .With(a => a.Project = project);
        var kimaiEntry = BuildTimeEntry(activity, Today);
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_CustomerDeactivated_False()
    {
        //Arrange
        var customer = DefaultActivity.Project.Customer.Copy()
            .With(c => c.Id = Sequence.CustomerId.Next())
            .With(c => c.Visible = false);
        var project = DefaultActivity.Project.Copy()
            .With(p => p.Id = Sequence.ProjectId.Next())
            .With(p => p.Customer = customer);
        var activity = DefaultActivity.Copy()
            .With(a => a.Id = Sequence.ActivityId.Next())
            .With(a => a.Project = project);
        var kimaiEntry = BuildTimeEntry(activity, Today);
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Single().CanExport.ShouldBeFalse();
    }

    #endregion Export
    
    #region Comments

    [TestMethod]
    public async Task NoSpecialComments()
    {
        //Arrange
        BuildTimeEntry().Description = "Drank coffee";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldBeNull();
        entry.Accomplishments.ShouldBeNull();
        entry.Impediments.ShouldBeNull();
        entry.Learnings.ShouldBeNull();
        entry.OtherComments.ShouldBe("Drank coffee");
    }
    
    [TestMethod]
    public async Task Accomplishment()
    {
        //Arrange
        BuildTimeEntry().Description = "🏆 Drank coffee";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldBeNull();
        entry.Accomplishments.ShouldBe("Drank coffee");
        entry.Impediments.ShouldBeNull();
        entry.Learnings.ShouldBeNull();
        entry.OtherComments.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task MixedComments()
    {
        //Arrange
        BuildTimeEntry().Description = """
                                       🏆 Drank coffee
                                       🧱Bathroom queues
                                       🧠 Bladder control
                                       """;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldBeNull();
        entry.Accomplishments.ShouldBe("Drank coffee");
        entry.Impediments.ShouldBe("Bathroom queues");
        entry.Learnings.ShouldBe("Bladder control");
        entry.OtherComments.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task MultipleComments()
    {
        //Arrange
        BuildTimeEntry().Description = """
                                       🏆 Drank coffee
                                       🧱Bathroom queues
                                       🧠 Bladder control
                                       Ticket#1234
                                       🏆 Also, fixed a bug
                                       """;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldBeNull();
        entry.Accomplishments.ShouldBe("""
                                       Drank coffee
                                       Also, fixed a bug
                                       """
        );
        entry.Impediments.ShouldBe("Bathroom queues");
        entry.Learnings.ShouldBe("Bladder control");
        entry.OtherComments.ShouldBe("Ticket#1234");
    }

    #endregion Comments

    #region WorkItems

    [TestMethod]
    public async Task WorkItem_IdOnly()
    {
        //Arrange
        BuildTimeEntry().Description = "D#12345";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Accomplishments.ShouldBeNull();
        entry.Impediments.ShouldBeNull();
        entry.Learnings.ShouldBeNull();
        entry.OtherComments.ShouldBeNull();

        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(12345);
        entry.Task.Title.ShouldBeNullOrEmpty();
        entry.Task.Type.ShouldBe(WorkItemType.Unknown);
        entry.Task.Parent.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task WorkItemTitle()
    {
        //Arrange
        BuildTimeEntry().Description = "D#12345 Program should start without crash";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Accomplishments.ShouldBeNull();
        entry.Impediments.ShouldBeNull();
        entry.Learnings.ShouldBeNull();
        entry.OtherComments.ShouldBeNull();

        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(12345);
        entry.Task.Title.ShouldBe("Program should start without crash");
        entry.Task.Type.ShouldBe(WorkItemType.Unknown);
        entry.Task.Parent.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task WorkItemRobustness()
    {
        //Arrange
        BuildTimeEntry().Description = "d12345 - Program should start without crash";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Accomplishments.ShouldBeNull();
        entry.Impediments.ShouldBeNull();
        entry.Learnings.ShouldBeNull();
        entry.OtherComments.ShouldBeNull();

        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(12345);
        entry.Task.Title.ShouldBe("Program should start without crash");
        entry.Task.Type.ShouldBe(WorkItemType.Unknown);
        entry.Task.Parent.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task WorkItemUrl()
    {
        //Arrange
        BuildTimeEntry().Description = "D#12345 Program should start without crash";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        var expectedUrl = AzureDevOps.AsInterface().ConnectionSettings.Url.AppendPathSegments("_workItems", "edit", "12345");
        entry.Task.Url.ShouldBe(expectedUrl);
    }
    
    [TestMethod]
    public async Task WorkItemWithParent()
    {
        //Arrange
        BuildTimeEntry().Description = "D#12345 Program should start without crash D#12346 Coding";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();

        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(12346);
        entry.Task.Title.ShouldBe("Coding");
        entry.Task.Type.ShouldBe(WorkItemType.Unknown);

        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(12345);
        entry.Task.Parent.Title.ShouldBe("Program should start without crash");
    }

    #endregion WorkItems

}
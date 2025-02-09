using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.ExportPayloads;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.Models;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class ExportDailyStandUpTests : DailyStandUpTests
{

    public ExportDailyStandUpTests()
    {
        ActivityUnderTest = TestActivities.SingleRandom();
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    private Activity ActivityUnderTest { get; }
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }
    
    private KimaiTimeEntry BuildTimeEntry() => BuildTimeEntry(ActivityUnderTest, Today);

    private async Task<DaySummary> GetDayAsync()
    {
        var period = await Server.GetStandUpPeriodAsync(Today, Today);
        await Server.GetWorkItemsAsync(period);

        return period.Days.Single(day => day.Date == Today);
    }

    private WorkItem BuildTask()
    {
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);

        task.Fields.AssignedTo = AzureDevOps.Identity.ToUser();

        return task;
    }

    #endregion Arrange

    #region Act

    private async Task<TimeEntry[]> ExportTimeEntriesAsync(params KimaiTimeEntry[] kimaiEntries)
    {
        //Arrange
        var day = await GetDayAsync();
        var timeEntries = day
            .Projects
            .SelectMany(p => p.Activities)
            .SelectMany(a => a.TimeEntries)
            .Where(x => x.Id.IsIn(kimaiEntries.Select(k => k.Id)))
            .ToArray();

        //Act
        await Server.ExportAsync(timeEntries);

        return timeEntries;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        entries.Length.ShouldBe(1);
        entries.Single().Exported.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task KimaiUpdated()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Exported.ShouldBeFalse();

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        kimaiEntry.Exported.ShouldBeTrue();
    }
    
    #region Task Adjustment

    [TestMethod]
    public async Task NoTask_TaskAdjustmentNotSent()
    {
        //Arrange
        var task = BuildTask();
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Description = null;

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        TaskAdjustmentExporter.FindOrDefault(task.Id).ShouldBeNull();
    }

    [TestMethod]
    public async Task TaskAdjustmentSent()
    {
        //Arrange
        var task = BuildTask();
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var totalTime = kimaiEntry.End!.Value - kimaiEntry.Begin;
        TaskAdjustmentExporter.Find(task.Id).Adjustment.ShouldBe(totalTime);
    }
    
    [TestMethod]
    public async Task TaskAdjustmentFails_KimaiNotUpdated()
    {
        //Arrange
        var task = BuildTask();
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        TaskAdjustmentExporter.ThrowOnSend = true;

        //Act
        await Should.ThrowAsync<ApplicationException>(async () => await ExportTimeEntriesAsync(kimaiEntry));

        //Assert
        kimaiEntry.Exported.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AlreadyExported_NotExportedAgain()
    {
        //Arrange
        var task = BuildTask();
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);
        kimaiEntry.Exported = true;

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        TaskAdjustmentExporter.FindOrDefault(task.Id).ShouldBeNull();
    }

    /// <summary>
    /// Ensure that if multiple time entries for the same AzDO task are exported together that they are summed into 1 export record (<see cref="TaskAdjustment"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Aggregating the time entries into a single task adjustment isn't just for efficiency in reducing the number of messages.
    /// It also minimizes rounding errors.
    /// The adjustment should be made in increments of 0.1 hours (6 minutes).
    /// AzDO task cards on their sprint boards can show the Remaining Work field.
    /// But the have is often cut off if it is 2 decimal points.  So to avoid that we limit the numbers to one decimal point.
    /// </para>
    /// </remarks>
    /// <returns></returns>
    [TestMethod]
    public async Task TaskTotalSummed()
    {
        //Arrange
        var task = BuildTask();

        var activity = TestActivities.SingleRandom();
        var today = Today;
        
        var kimaiEntry1 = BuildTimeEntry(activity, today, TimeSpan.FromMinutes(2)).AddWorkItems(task);
        var kimaiEntry2 = BuildTimeEntry(activity, today, TimeSpan.FromMinutes(3)).AddWorkItems(task);
        var kimaiEntry3 = BuildTimeEntry(activity, today, TimeSpan.FromMinutes(6)).AddWorkItems(task);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry1, kimaiEntry2, kimaiEntry3);

        //Assert
        TaskAdjustmentExporter.Find(task.Id).Adjustment.ShouldBe(TimeSpan.FromMinutes(11));
    }

    /// <summary>
    /// Timing other people's tasks doesn't count for export to AzDO.  Time Remaining only applies to the time spent by the assigned person.
    /// If other people need to help with that task, other tasks should be created.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task AssignedToOther_NotAdjusted()
    {
        //Arrange
        var task = BuildTask();
        task.Fields.AssignedTo = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        TaskAdjustmentExporter.FindOrDefault(task.Id).ShouldBeNull();
    }
    
    [TestMethod]
    public async Task AssignedToOther_IsExported()
    {
        //Arrange
        var task = BuildTask();
        task.Fields.AssignedTo = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        kimaiEntry.Exported.ShouldBeTrue();
    }

    #endregion Task Adjustment

    #region ViewModel Updates

    #region Task Time

    [TestMethod]
    public async Task TaskRemainingWorkUpdated()
    {
        //Arrange
        var task = BuildTask();
        var remainingWork = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = remainingWork.TotalHours;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var entry = entries.Single();
        var entryDuration = kimaiEntry.End!.Value - kimaiEntry.Begin;
        entry.Task!.RemainingWork.ShouldBe(remainingWork - entryDuration);
    }
    
    [TestMethod]
    public async Task TaskRemainingWork_Null_Unchanged()
    {
        //Arrange
        var task = BuildTask();
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var entry = entries.Single();
        entry.Task!.RemainingWork.ShouldBeNull();
    }

    [TestMethod]
    public async Task TaskCompletedWorkUpdated()
    {
        //Arrange
        var task = BuildTask();
        var completedWork = TimeSpan.FromHours(1).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.CompletedWork = completedWork.TotalHours;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var entry = entries.Single();
        var entryDuration = kimaiEntry.End!.Value - kimaiEntry.Begin;
        entry.Task!.CompletedWork.ShouldBe(completedWork + entryDuration);
    }
    
    [TestMethod]
    public async Task TaskCompletedWork_Null_Incremented()
    {
        //Arrange
        var task = BuildTask();
        task.Fields.CompletedWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var entry = entries.Single();
        var entryDuration = kimaiEntry.End!.Value - kimaiEntry.Begin;
        entry.Task!.CompletedWork.ShouldBe(entryDuration);
    }

    #endregion Task Time

    #region Parent Updated

    [TestMethod]
    public async Task TimeEntry_CanExport_Reset()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var entry = entries.Single();
        entry.CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task Parents_CanExport_Reset()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var entry = entries.Single();
        entry.ParentTaskSummary.ShouldNotBeNull();
        entry.ParentTaskSummary.CanExport.ShouldBeFalse();
        entry.ParentActivitySummary.CanExport.ShouldBeFalse();
        entry.ParentActivitySummary.ParentProjectSummary.CanExport.ShouldBeFalse();
        entry.ParentActivitySummary.ParentProjectSummary.ParentDay.CanExport.ShouldBeFalse();
        entry.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod.CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task MultipleActivityEntries_ExportOne_ActivityCanStillExport()
    {
        //Arrange
        var activity = TestActivities.SingleRandom();
        var kimaiEntry1 = BuildTimeEntry(activity);
        var kimaiEntry2 = BuildTimeEntry(activity);

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry1);

        //Assert
        kimaiEntry1.Exported.ShouldBeTrue();
        kimaiEntry2.Exported.ShouldBeFalse();

        var entry = entries.Single();
        entry.CanExport.ShouldBeFalse();
        entry.ParentActivitySummary.CanExport.ShouldBeTrue();
        entry.ParentActivitySummary.ParentProjectSummary.CanExport.ShouldBeTrue();
        entry.ParentActivitySummary.ParentProjectSummary.ParentDay.CanExport.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task CanExport_TortureTest()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        var activity1 = TestActivities.SingleRandom();
        var workItem = BuildTask();
        var kimaiEntry1 = BuildTimeEntry(activity1, today).AddWorkItems(workItem);
        var kimaiEntry2 = BuildTimeEntry(activity1, today);

        var activity2 = TestActivities.Where(a => a.Project != activity1.Project).SingleRandom();
        var kimaiEntry3 = BuildTimeEntry(activity2, today);
        var kimaiEntry4 = BuildTimeEntry(activity2, yesterday);
        
        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry1, kimaiEntry3);

        //Assert
        entries.Length.ShouldBe(2);

        kimaiEntry1.Exported.ShouldBeTrue();
        kimaiEntry2.Exported.ShouldBeFalse();
        kimaiEntry3.Exported.ShouldBeTrue();
        kimaiEntry4.Exported.ShouldBeFalse();

        var entry1 = entries.Single(x => x.Id == kimaiEntry1.Id);
        entry1.CanExport.ShouldBeFalse();
        entry1.ParentTaskSummary.ShouldNotBeNull();
        entry1.ParentTaskSummary.Task.ShouldNotBeNull();
        entry1.ParentTaskSummary.Task.Id.ShouldBe(workItem.Id);
        entry1.ParentTaskSummary.CanExport.ShouldBeFalse();
        entry1.ParentTaskSummary.AllExported.ShouldBeTrue();
        entry1.ParentActivitySummary.CanExport.ShouldBeTrue();
        entry1.ParentActivitySummary.AllExported.ShouldBeFalse();
        entry1.ParentActivitySummary.ParentProjectSummary.CanExport.ShouldBeTrue();
        entry1.ParentActivitySummary.ParentProjectSummary.AllExported.ShouldBeFalse();
        entry1.ParentActivitySummary.ParentProjectSummary.ParentDay.CanExport.ShouldBeTrue();
        entry1.ParentActivitySummary.ParentProjectSummary.ParentDay.AllExported.ShouldBeFalse();
        entry1.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod.CanExport.ShouldBeTrue();
        entry1.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod.AllExported.ShouldBeFalse();

        var entry3 = entries.Single(x => x.Id == kimaiEntry3.Id);
        entry3.CanExport.ShouldBeFalse();
        entry3.ParentTaskSummary.ShouldNotBeNull();
        entry3.ParentTaskSummary.Task.ShouldBeNull();
        entry3.ParentTaskSummary.CanExport.ShouldBeFalse();
        entry3.ParentTaskSummary.AllExported.ShouldBeTrue();
        entry3.ParentActivitySummary.CanExport.ShouldBeFalse();
        entry3.ParentActivitySummary.AllExported.ShouldBeTrue();
        entry3.ParentActivitySummary.ParentProjectSummary.CanExport.ShouldBeFalse();
        entry3.ParentActivitySummary.ParentProjectSummary.AllExported.ShouldBeTrue();
        entry3.ParentActivitySummary.ParentProjectSummary.ParentDay.CanExport.ShouldBeTrue();
        entry3.ParentActivitySummary.ParentProjectSummary.ParentDay.AllExported.ShouldBeFalse();
        entry3.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod.CanExport.ShouldBeTrue();
        entry3.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod.AllExported.ShouldBeFalse();
    }

    #endregion Parent Updated

    #endregion ViewModel Updates

    #region Daily Activity Export

    [TestMethod]
    public async Task DailyActivitySent()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Activity.Project.ShouldNotBeNull();

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var payload = DailyActivityExporter.Messages.SingleOrDefault(msg => msg.Date == Today && msg.ActivityId == kimaiEntry.Activity.Id);
        payload.ShouldNotBeNull();

        payload.ActivityName.ShouldBe(kimaiEntry.Activity.Name);
        payload.ActivityDescription.ShouldBe(kimaiEntry.Activity.Comment);
        payload.ProjectId.ShouldBe(kimaiEntry.Activity.Project.Id);
        payload.ProjectName.ShouldBe(kimaiEntry.Activity.Project.Name);
        payload.CustomerId.ShouldBe(kimaiEntry.Activity.Project.Customer.Id);
        payload.CustomerName.ShouldBe(kimaiEntry.Activity.Project.Customer.Name);

        var totalTime = kimaiEntry.End!.Value - kimaiEntry.Begin;
        payload.TotalTime.ShouldBe(totalTime);
    }
    
    [TestMethod]
    public async Task DailyActivitySent_TimeRounded()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var payload = DailyActivityExporter.Messages.Single(msg => msg.Date == Today && msg.ActivityId == kimaiEntry.Activity.Id);

        var totalTime = kimaiEntry.End!.Value - kimaiEntry.Begin;
        payload.TotalTime.ShouldBe(totalTime);
    }
    
    [TestMethod]
    public async Task DailyActivitySent_TimeOnlyIncludesExported()
    {
        //Arrange
        var activity = TestActivities.SingleRandom();
        var kimaiEntry1 = BuildTimeEntry(activity).With(x => x.Exported = true);
        var kimaiEntry2 = BuildTimeEntry(activity);
        BuildTimeEntry(activity);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry2);

        //Assert
        var payload = DailyActivityExporter.Messages.Single(msg => msg.Date == Today && msg.ActivityId == activity.Id);

        var totalTime = (kimaiEntry1.End!.Value - kimaiEntry1.Begin)
                        + (kimaiEntry2.End!.Value - kimaiEntry2.Begin);
        payload.TotalTime.ShouldBe(totalTime);
    }

    [TestMethod]
    public async Task DailyActivitySent_Task()
    {
        //Arrange
        var task = BuildTask();
        var kimaiEntry = BuildTimeEntry().AddWorkItems(task);

        //Act
        var entries = await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var payload = DailyActivityExporter.Messages.Single(msg => msg.Date == Today && msg.ActivityId == kimaiEntry.Activity.Id);
        
        var workItem = entries.Single().Task?.Parent;
        workItem.ShouldNotBeNull();
        payload.Tasks.ShouldBe($"D#{workItem.Id} {workItem.Title} » D#{task.Id} {task.Fields.Title}");
    }
    
    [TestMethod]
    public async Task DailyActivitySent_FeatureParent_NotIncludedInComment()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var feature).AddChild(out var workItem);
        feature.Fields.WorkItemType = WorkItemType.Feature.ToApiValue();
        workItem.Fields.WorkItemType = WorkItemType.ProductBacklogItem.ToApiValue();
        var kimaiEntry = BuildTimeEntry().AddWorkItems(workItem);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var payload = DailyActivityExporter.Messages.Single(msg => msg.Date == Today && msg.ActivityId == kimaiEntry.Activity.Id);
        
        payload.Tasks.ShouldBe($"D#{workItem.Id} {workItem.Fields.Title}");
    }
    
    [TestMethod]
    public async Task DailyActivitySent_Comments()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Description = """
                                 🏆 Drank coffee
                                 🧱Bathroom queues
                                 🧠 Bladder control
                                 Stand-Up Meeting
                                 """;

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        var payload = DailyActivityExporter.Messages.Single(msg => msg.Date == Today && msg.ActivityId == kimaiEntry.Activity.Id);
        
        payload.Accomplishments.ShouldBe("Drank coffee");
        payload.Impediments.ShouldBe("Bathroom queues");
        payload.Learnings.ShouldBe("Bladder control");
        payload.OtherComments.ShouldBe("Stand-Up Meeting");
    }

    #endregion Daily Activity Export
}
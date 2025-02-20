﻿using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class WorkItemDailyStandUpTests : DailyStandUpTests
{

    public WorkItemDailyStandUpTests()
    {
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    #region Helpers

    #region Arrange
    
    private KimaiTimeEntry BuildTimeEntry() => BuildTimeEntry(TestActivities.SingleRandom());

    #endregion Arrange

    #region Act

    private async Task<TimeEntry[]> GetTimesAsync()
    {
        var today = Today;
        var period = await Server.GetStandUpPeriodAsync(today.AddDays(-6), today);
        await Server.GetWorkItemsAsync(period);
        
        return period.Days.SelectMany(day => day.Projects.SelectMany(project => project.Activities.SelectMany(activity => activity.TimeEntries)))
            .ToArray();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        kimaiEntry.AddWorkItems(workItem, task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Length.ShouldBe(1);
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(workItem.Id);
    }

    [TestMethod]
    public async Task AzureDevOpsDisabled_NoTask()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        kimaiEntry.AddWorkItems(workItem, task);
        AzureDevOps.Enabled = false;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Length.ShouldBe(1);
        var entry = entries.Single();
        entry.Task.ShouldBeNull();
    }

    #region Load Work Item Type and Parent/Child relations

    [TestMethod]
    public async Task TaskType()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.WorkItemType.ShouldBe(WorkItemType.Task.ToApiValue());
        kimaiEntry.AddWorkItems(task);
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Type.ShouldBe(WorkItemType.Task);
    }
    
    [TestMethod]
    public async Task Parent()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        kimaiEntry.AddWorkItems(task);
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Type.ShouldBe(WorkItemType.Task);
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task ParentType()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        kimaiEntry.AddWorkItems(task);
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Type.ToApiValue().ShouldBe(workItem.Fields.WorkItemType);
    }
    
    [TestMethod]
    public async Task TimeEntryReferencesBoardWorkItem()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);
        kimaiEntry.AddWorkItems(workItem);
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(workItem.Id);
        entry.Task.Type.ToApiValue().ShouldBe(workItem.Fields.WorkItemType);
        entry.Task.Parent.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task OrderOfWorkItemIdsReversed()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        kimaiEntry.Description = $"D#{task.Id} Task Title » D#{workItem.Id} Board Item Title";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task TaskReferencedTwice()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        kimaiEntry.Description = $"D#{task.Id} Misquoted PBI ID » D#{task.Id} Task Title";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task BoardItemReferencedTwice()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);
        kimaiEntry.Description = $"D#{workItem.Id} Board Item Title » D#{workItem.Id} Misquoted Task ID";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(workItem.Id);
        entry.Task.Parent.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task OrphanedTask()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var task);
        task.Fields.WorkItemType = WorkItemType.Task.ToApiValue();
        kimaiEntry.Description = $"D#{task.Id}";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Parent.ShouldBeNull();
    }
    
    /// <summary>
    /// If someone enters a bad/unknown D# in the Kimai comment, don't throw it away.  Show that the D# is Unknown.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task TaskDoesNotExist_ShowUnknown()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Description = "D#99999";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(99999);
        entry.Task.Type.ShouldBe(WorkItemType.Unknown);
    }
    
    /// <summary>
    /// If someone enters a bad/unknown D# in the Kimai comment and a good one, keep the good one.
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task TaskDoesNotExist_KeepGoodOne()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var task);
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Description = $"D#99999 » D#{task.Id}";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Type.ShouldBe(WorkItemType.FromApiValue(task.Fields.WorkItemType));
    }
    
    [TestMethod]
    public async Task OneTaskDoesNotExist()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var task);
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Description = $"D#99999 Not Found » D#{task.Id}";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
    }

    [TestMethod]
    public async Task UnknownTaskType()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var task);
        task.Fields.WorkItemType = "Test Case";
        kimaiEntry.Description = $"D#{task.Id}";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Type.ShouldBe(WorkItemType.Unknown);
    }
    
    [TestMethod]
    public async Task UnknownTaskState()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var task);
        task.Fields.WorkItemType = "Test Case";
        task.Fields.State = "Design";
        kimaiEntry.Description = $"D#{task.Id}";
        
        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.State.ShouldBe(ScrumState.Unknown);
    }

    [TestMethod]
    public async Task FeatureAndBug_ReferencesTheBug()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        var builder = AzureDevOpsBuilder.BuildWorkItem(out var feature);
        feature.Fields.WorkItemType = WorkItemType.Feature.ToApiValue();
        builder.AddChild(out var workItem);
        WorkItemType.FromApiValue(workItem.Fields.WorkItemType).ShouldBeOneOf(WorkItemType.BoardTypes.ToArray());
        kimaiEntry.Description = $"D#{feature.Id} {feature.Fields.Title} » D#{workItem.Id} {workItem.Fields.Title}";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Length.ShouldBe(1);
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(workItem.Id);
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(feature.Id);
    }
    
    [TestMethod]
    public async Task EpicAndFeature_ReferencesTheFeature()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        var builder = AzureDevOpsBuilder.BuildWorkItem(out var epic);
        epic.Fields.WorkItemType = WorkItemType.Epic.ToApiValue();
        builder.AddChild(out var feature);
        WorkItemType.FromApiValue(feature.Fields.WorkItemType).ShouldBe(WorkItemType.Feature);
        kimaiEntry.Description = $"D#{epic.Id} {epic.Fields.Title} » D#{feature.Id} {feature.Fields.Title}";

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Length.ShouldBe(1);
        var entry = entries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(feature.Id);
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(epic.Id);
    }

    #endregion Load Work Item Type and Parent/Child relations

    #region Time Remaining

    [TestMethod]
    public async Task TimeRemaining_KimaiAllExported_ReportsSameAsAzureDevOps()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.OriginalEstimate = estimate.TotalHours + 2.0;
        task.Fields.RemainingWork = estimate.TotalHours;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);
        kimaiEntry.Exported = true;
        task.Fields.State = ScrumState.InProgress.ToApiValue();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.TimeRemaining.ShouldBe(estimate);
    }
    
    [TestMethod]
    public async Task TimeRemaining_TimeRemainingMissing_ReportsOriginalEstimate()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.OriginalEstimate = estimate.TotalHours;
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);
        kimaiEntry.Exported = true;
        task.Fields.State = ScrumState.InProgress.ToApiValue();

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.TimeRemaining.ShouldBe(estimate);
    }
    
    [TestMethod]
    public async Task TimeRemaining_SubtractsUnexported()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = estimate.TotalHours;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);
        kimaiEntry.End.ShouldNotBeNull();
        kimaiEntry.Exported = false;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        var duration = kimaiEntry.End.Value - kimaiEntry.Begin;
        entry.TimeRemaining.ShouldBe(estimate - duration);
    }
    
    [TestMethod]
    public async Task TimeRemaining_SubtractsAllUnexported()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = estimate.TotalHours;

        var entry1 = BuildTimeEntry();
        entry1.AddWorkItems(task);
        entry1.End.ShouldNotBeNull();
        entry1.Exported = false;

        var entry2 = BuildTimeEntry();
        entry2.AddWorkItems(task);
        entry2.End.ShouldNotBeNull();
        entry2.Exported = true;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var duration = entry1.End.Value - entry1.Begin;
        var expected = estimate - duration;

        entries.Length.ShouldBe(2);
        entries.ShouldAllBe(x => x.TimeRemaining == expected);
    }
    
    [TestMethod]
    public async Task RefreshTimeRemaining_UpdatesParentTaskSummary()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = estimate.TotalHours;

        var entry1 = BuildTimeEntry();
        entry1.AddWorkItems(task);
        entry1.End = null;
        entry1.Exported = false;

        var entries = await GetTimesAsync();
        var entry = entries.Single();
        entry.TimeRemaining.ShouldBe(estimate);
        entry.TotalTime.ShouldBe(TimeSpan.Zero);

        //Arrange - UI will update the TotalTime based on a timer for tasks that are currently running
        var now = entry.Begin + TimeSpan.FromMinutes(30).Randomize();

        //Act
        StandUpService.CascadeEndTimeChange(entry, now);

        //Assert
        var expected = estimate - entry.TotalTime;
        entry.TimeRemaining.ShouldBe(expected);
        entry.ParentTaskSummary.ShouldNotBeNull();
        entry.ParentTaskSummary.TimeRemaining.ShouldBe(expected);
    }
    
    [TestMethod]
    public async Task TimeRemaining_DifferentTasks()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task1);
        task1.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate1 = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task1.Fields.RemainingWork = estimate1.TotalHours;

        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task2);
        task2.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate2 = TimeSpan.FromHours(8).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task2.Fields.RemainingWork = estimate2.TotalHours;

        var entry1 = BuildTimeEntry();
        entry1.AddWorkItems(task1);
        entry1.End.ShouldNotBeNull();
        entry1.Exported = false;

        var entry2 = BuildTimeEntry();
        entry2.AddWorkItems(task2);
        entry2.End.ShouldNotBeNull();
        entry2.Exported = false;

        //Act
        var entries = await GetTimesAsync();

        //Assert
        entries.Length.ShouldBe(2);
        entries.Single(x => x.Id == entry1.Id).TimeRemaining.ShouldBe(estimate1 - (entry1.End.Value - entry1.Begin));
        entries.Single(x => x.Id == entry2.Id).TimeRemaining.ShouldBe(estimate2 - (entry2.End.Value - entry2.Begin));
    }

    [TestMethod]
    public async Task TimeRemaining_TaskDone_Null()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.Done.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = estimate.TotalHours;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.TimeRemaining.ShouldBeNull();
    }
    #endregion Time Remaining

    #region NeedsEstimate

    [TestMethod]
    public async Task NeedsEstimate_ToDo_NoEstimate_Yes()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.ToDo.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.NeedsEstimate.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task NeedsEstimate_ToDo_HasEstimate_No()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.ToDo.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.OriginalEstimate = estimate.TotalHours;
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.NeedsEstimate.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task NeedsEstimate_ToDo_HasRemaining_No()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.ToDo.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = estimate.TotalHours;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.NeedsEstimate.ShouldBeFalse();
    }

    [TestMethod]
    public async Task NeedsEstimate_InProgress_NoEstimate_Yes()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.NeedsEstimate.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task NeedsEstimate_Done_NoEstimate_No()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.Done.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.NeedsEstimate.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task NeedsEstimate_Removed_NoEstimate_No()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.Removed.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        var entries = await GetTimesAsync();

        //Assert
        var entry = entries.Single();
        entry.NeedsEstimate.ShouldBeFalse();
    }
    #endregion NeedsEstimate

    #region GetChildWorkItems

    [TestMethod]
    public async Task GetChildWorkItems_SmokeTest()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        var boardItem = await Server.GetWorkItemAsync(workItem.Id);
        boardItem.ShouldNotBeNull();
        boardItem.Children.Count.ShouldBe(1);
        boardItem.Children.Single().Id.ShouldBe(task.Id);
        boardItem.Children.Single().Type.ShouldBe(WorkItemType.Unknown);

        //Act
        await Server.GetChildWorkItemsAsync(boardItem);

        //Assert
        boardItem.Children.Single().Type.ShouldBe(WorkItemType.Task);
        boardItem.Children.Single().Id.ShouldBe(task.Id);
    }
    
    [TestMethod]
    public async Task GetChildWorkItems_NoChildren_NoApiCallNeeded()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);
        var boardItem = await Server.GetWorkItemAsync(workItem.Id);
        boardItem.ShouldNotBeNull();
        boardItem.Children.Count.ShouldBe(1);
        boardItem.Children.Single().Id.ShouldBe(task.Id);
        boardItem.Children.Single().Type.ShouldBe(WorkItemType.Unknown);

        //First call to load the children
        await Server.GetChildWorkItemsAsync(boardItem);

        //Act - subsequent calls should not call the API
        var wasCalled = false;
        AzureDevOps.OnGetWorkItems = _ => wasCalled = true;
        await Server.GetChildWorkItemsAsync(boardItem);

        //Assert
        wasCalled.ShouldBeFalse();
    }

    #endregion GetChildWorkItems
}

internal static class TimeEntryExtensions
{
    public static KimaiTimeEntry AddWorkItems(this KimaiTimeEntry timeEntry, params WorkItem[] workItems)
    {
        var tasks = workItems.Where(wi => wi.Fields.WorkItemType == WorkItemType.Task.ToApiValue()).ToArray();
        var boardItemTypes = WorkItemType.BoardTypes.Select(x => x.ToApiValue());
        var boardItems = workItems.Where(wi => wi.Fields.WorkItemType.IsIn(boardItemTypes)).ToArray();
        var otherItems = workItems.Except(tasks).Except(boardItems).ToArray();

        List<string> lines;

        if (tasks.Length == 1 && boardItems.Length == 1 && otherItems.Length == 0)
        {
            lines = [$"D#{boardItems.Single().Id} {boardItems.Single().Fields.Title} » D#{tasks.Single().Id} {tasks.Single().Fields.Title}"];
        }
        else
        {
            lines = workItems.Select(wi => $"D#{wi.Id} {wi.Fields.Title}").ToList();
        }

        timeEntry.Description += string.Join(Environment.NewLine, lines);

        return timeEntry;
    }
}
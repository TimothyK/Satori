﻿using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Services;
using Satori.AppServices.Extensions;
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
        var srv = new StandUpService(Kimai.AsInterface(), AzureDevOps.AsInterface());
        var days = await srv.GetStandUpDaysAsync(today.AddDays(-6), today);
        await srv.GetWorkItemsAsync(days);
        
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

    #endregion Load Work Item Type and Parent/Child relations

    #region Time Remaining

    [TestMethod]
    public async Task TimeRemaining_KimaiAllExported_ReportsSameAsAzureDevOps()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        kimaiEntry.AddWorkItems(task);

        kimaiEntry.Exported = true;
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.OriginalEstimate = estimate.TotalHours + 2.0;
        task.Fields.RemainingWork = estimate.TotalHours;

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
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        kimaiEntry.AddWorkItems(task);

        kimaiEntry.Exported = true;
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.OriginalEstimate = estimate.TotalHours;
        task.Fields.RemainingWork = null;

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
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        kimaiEntry.AddWorkItems(task);
        kimaiEntry.End.ShouldNotBeNull();

        kimaiEntry.Exported = false;
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var estimate = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = estimate.TotalHours;

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

    #endregion Time Remaining

}

internal static class TimeEntryExtensions
{
    public static void AddWorkItems(this KimaiTimeEntry timeEntry, params WorkItem[] workItems)
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
    }
}
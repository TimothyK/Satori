using CodeMonkeyProjectiles.Linq;
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
        task.Fields.WorkItemType.ShouldBe(WorkItemType.Task.ToApiValue());
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
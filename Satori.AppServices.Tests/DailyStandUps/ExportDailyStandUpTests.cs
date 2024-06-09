using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai.Models;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;

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

    private async Task<StandUpDay> GetDayAsync()
    {
        var days = await Server.GetStandUpDaysAsync(Today, Today);
        await Server.GetWorkItemsAsync(days);

        return days.Single(day => day.Date == Today);
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
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.Description = null;

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        TaskAdjuster.FindOrDefault(task.Id).ShouldBeNull();
    }
    
    [TestMethod]
    public async Task TaskAdjustmentSent()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        //Act
        await ExportTimeEntriesAsync(kimaiEntry);

        //Assert
        TaskAdjuster.Find(task.Id).Adjustment.ShouldBe(kimaiEntry.End!.Value - kimaiEntry.Begin);
    }
    
    [TestMethod]
    public async Task TaskAdjustmentFails_KimaiNotUpdated()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var kimaiEntry = BuildTimeEntry();
        kimaiEntry.AddWorkItems(task);

        TaskAdjuster.ThrowOnSend = true;

        //Act
        await Should.ThrowAsync<ApplicationException>(async () => await ExportTimeEntriesAsync(kimaiEntry));

        //Assert
        kimaiEntry.Exported.ShouldBeFalse();
    }

    #endregion Task Adjustment

    #region ViewModel Updates

    #region Task Time

    [TestMethod]
    public async Task TaskRemainingWorkUpdated()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
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
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
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
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
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
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
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

    #endregion Parent Updated

    #endregion ViewModel Updates

}
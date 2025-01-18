using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class TimerTests : DailyStandUpTests
{
    public TimerTests()
    {
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    #region Helpers

    #region Arrange

    private KimaiTimeEntry BuildTimeEntry() => 
        BuildTimeEntry(Today)
            .With(t => t.End = null);

    #endregion Arrange

    #region Act

    private async Task<TimeEntry> StopTimerAsync(KimaiTimeEntry kimaiTimeEntry)
    {
        //Arrange
        var day = DateOnly.FromDateTime(kimaiTimeEntry.Begin.DateTime);
        var period = await GetPeriodAsync(day, day);
        await Server.GetWorkItemsAsync(period);

        var timeEntry = period.TimeEntries.Single(t => t.Id == kimaiTimeEntry.Id);
        
        //Act
        await Server.StopTimerAsync(timeEntry);

        //Assert
        return timeEntry;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest_Stop()
    {
        //Arrange
        var kimaiTimeEntry = BuildTimeEntry();

        //Act
        await StopTimerAsync(kimaiTimeEntry);

        //Assert
        kimaiTimeEntry.End.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Stop_SetsEnd()
    {
        //Arrange
        var timeEntry = BuildTimeEntry();

        //Act
        var actual = await StopTimerAsync(timeEntry);

        //Assert
        actual.End.ShouldBe(timeEntry.End);
    }

    [TestMethod]
    public async Task Stop_SetsIsRunning()
    {
        //Arrange
        var timeEntry = BuildTimeEntry();

        //Act
        var actual = await StopTimerAsync(timeEntry);

        //Assert
        actual.IsRunning.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Stop_SetsTotalTime()
    {
        //Arrange
        var timeEntry = BuildTimeEntry();

        //Act
        var actual = await StopTimerAsync(timeEntry);

        //Assert
        actual.End.ShouldNotBeNull();
        var expected = actual.End.Value - actual.Begin;
        actual.TotalTime.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Stop_SetsTotalTimeOnAllParents()
    {
        //Arrange
        var timeEntry = BuildTimeEntry();

        //Act
        var actual = await StopTimerAsync(timeEntry);

        //Assert
        actual.End.ShouldNotBeNull();
        var expected = actual.End.Value - actual.Begin;
        actual.ParentTaskSummary.ShouldNotBeNull();
        actual.ParentTaskSummary.TotalTime.ShouldBe(expected);
        actual.ParentActivitySummary.TotalTime.ShouldBe(expected);
        actual.ParentActivitySummary.ParentProjectSummary.TotalTime.ShouldBe(expected);
        actual.ParentActivitySummary.ParentProjectSummary.ParentDay.TotalTime.ShouldBe(expected);
        actual.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod.TotalTime.ShouldBe(expected);
    }
    
    [TestMethod]
    public async Task Stop_ResetsTimeRemaining()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var remaining = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = remaining.TotalHours;

        var timeEntry = BuildTimeEntry();
        timeEntry.AddWorkItems(task);

        //Act
        var actual = await StopTimerAsync(timeEntry);

        //Assert
        var expected = remaining - actual.TotalTime;
        actual.TimeRemaining.ShouldBe(expected);
        actual.ParentTaskSummary.ShouldNotBeNull();
        actual.ParentTaskSummary.TimeRemaining.ShouldBe(expected);
    }
    
    [TestMethod]
    public async Task Stop_MultipleTimeEntriesForTask_ResetsTimeRemainingOnAllEntries()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var remaining = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = remaining.TotalHours;

        var timeEntry1 = BuildTimeEntry(Today);
        timeEntry1.AddWorkItems(task);

        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task2);
        task2.Fields.State = ScrumState.InProgress.ToApiValue();
        var task2Remaining = remaining.Add(TimeSpan.FromHours(2));
        task2.Fields.RemainingWork = task2Remaining.TotalHours;
        var timeEntry2 = BuildTimeEntry(Today);
        timeEntry2.AddWorkItems(task2);

        var timeEntry3 = BuildTimeEntry();
        timeEntry3.AddWorkItems(task);
        
        //Act
        var stoppedTimeEntryViewModel = await StopTimerAsync(timeEntry3);

        //Assert
        var period = stoppedTimeEntryViewModel.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod;
        
        var task1TimeEntries = period.TimeEntries.Where(t => t.Task?.Id == task.Id).ToArray();
        task1TimeEntries.Length.ShouldBe(2);
        task1TimeEntries.Select(t => t.Id).ShouldContain(timeEntry1.Id);
        task1TimeEntries.Select(t => t.Id).ShouldContain(timeEntry3.Id);

        var expected = remaining - task1TimeEntries.Select(t => t.TotalTime).Sum();

        foreach (var actual in task1TimeEntries)
        {
            actual.TimeRemaining.ShouldBe(expected);
            actual.ParentTaskSummary.ShouldNotBeNull();
            actual.ParentTaskSummary.TimeRemaining.ShouldBe(expected);
        }

        var actualTask2 = period.TimeEntries.Single(t => t.Id == timeEntry2.Id);
        actualTask2.TimeRemaining.ShouldBe(task2Remaining - actualTask2.TotalTime);
    }
    
    [TestMethod]
    public async Task Stop_ExportedTime_DoesNotImpactTimeRemainingChange()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        var remaining = TimeSpan.FromHours(4).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = remaining.TotalHours;

        var timeEntry1 = BuildTimeEntry(Today).With(t => t.Exported = true);
        timeEntry1.AddWorkItems(task);

        var timeEntry2 = BuildTimeEntry();
        timeEntry2.AddWorkItems(task);
        
        //Act
        var stoppedTimeEntryViewModel = await StopTimerAsync(timeEntry2);

        //Assert
        var period = stoppedTimeEntryViewModel.ParentActivitySummary.ParentProjectSummary.ParentDay.ParentPeriod;
        
        var task1TimeEntries = period.TimeEntries.Where(t => t.Task?.Id == task.Id).ToArray();
        task1TimeEntries.Length.ShouldBe(2);
        task1TimeEntries.Select(t => t.Id).ShouldContain(timeEntry1.Id);
        task1TimeEntries.Select(t => t.Id).ShouldContain(timeEntry2.Id);

        var expected = remaining - stoppedTimeEntryViewModel.TotalTime;

        foreach (var actual in task1TimeEntries)
        {
            actual.TimeRemaining.ShouldBe(expected);
            actual.ParentTaskSummary.ShouldNotBeNull();
            actual.ParentTaskSummary.TimeRemaining.ShouldBe(expected);
        }
    }


}
using CodeMonkeyProjectiles.Linq;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;
using TimeEntry = Satori.AppServices.ViewModels.DailyStandUps.TimeEntry;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class TimerTests : DailyStandUpTests
{
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
}
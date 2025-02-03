using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AlertServices;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
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
        var entry = Builder.Builder<TimeEntry>.New().Build(t =>
        {
            t.Id = Sequence.TimeEntryId.Next();
            t.User = Kimai.CurrentUser;
        }, int.MaxValue);
        entry.Activity.Project = entry.Project;

        Kimai.AddTimeEntry(entry);

        return entry;
    }

    #endregion Arrange

    #region Act

    private async Task<TimeEntry> RestartTimerAsync(int entryId)
    {   
        var userService = new UserService(AzureDevOps.AsInterface(), Kimai.AsInterface(), AlertService);
        var timerServer = new TimerService(Kimai.AsInterface(), userService, AlertService);

        //Act
        await timerServer.RestartTimerAsync(entryId);

        //Assert
        var newEntry = Kimai.GetLastEntry();
        newEntry.ShouldNotBeNull();
        return newEntry;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var entry = BuildTimeEntry();
        var startTime = DateTimeOffset.Now.TruncateSeconds();

        //Act
        var actual = await RestartTimerAsync(entry.Id);

        //Assert
        var endTime = DateTimeOffset.Now;
        actual.Id.ShouldBeGreaterThan(entry.Id);
        actual.Project.Id.ShouldBe(entry.Project.Id);
        actual.Activity.Id.ShouldBe(entry.Activity.Id);
        actual.User.Id.ShouldBe(entry.User.Id);
        actual.Begin.ShouldBeGreaterThanOrEqualTo(startTime);
        actual.Begin.ShouldBeLessThanOrEqualTo(endTime);
        actual.End.ShouldBeNull();
    }
}
using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Tests.Extensions;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
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

    //private KimaiTimeEntry BuildTimeEntry(TimeSpan duration) => BuildTimeEntry(DefaultActivity, Today, duration);

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
    
    #endregion Time

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

    #endregion Comments

}
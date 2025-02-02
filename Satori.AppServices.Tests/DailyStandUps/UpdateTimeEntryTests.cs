using Flurl;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class UpdateTimeEntryTests : DailyStandUpTests
{
    #region Helpers

    #region Arrange

    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    public UpdateTimeEntryTests()
    {
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    private KimaiTimeEntry BuildTimeEntry() => BuildTimeEntry(RandomGenerator.PickOne(TestActivities));

    #endregion Arrange

    #region Act

    private async Task<PeriodSummary> UpdateDescriptionAsync(KimaiTimeEntry entry, string description)
    {
        //Arrange
        var day = DateOnly.FromDateTime(entry.Begin.DateTime);
        var period = await GetPeriodAsync(day, day);
        var originalDaySummary = period.Days.Single();
        var descriptionMap = new Dictionary<int, string>
        {
            { entry.Id, description }
        };

        //Act
        await Server.UpdateTimeEntryDescriptionAsync(period.Days.Single(), descriptionMap);

        //Assert
        period.Days.Single().ShouldNotBeSameAs(originalDaySummary); //The view model should be reloaded

        return period;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var period = await UpdateDescriptionAsync(kimaiEntry, "Drink Coffee");

        //Assert
        var entry = period.TimeEntries.Single();
        entry.OtherComments.ShouldBe("Drink Coffee");
    }

    [TestMethod]
    public async Task KimaiIsUpdated()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        await UpdateDescriptionAsync(kimaiEntry, "Drink Coffee");

        //Assert
        var actual = Kimai.GetLastEntry(Today);
        actual.ShouldNotBeNull();
        actual.Description.ShouldBe("Drink Coffee");
    }

    [TestMethod]
    public async Task Accomplishment()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();

        //Act
        var period = await UpdateDescriptionAsync(kimaiEntry, "🏆Drink Coffee");

        //Assert
        var entry = period.TimeEntries.Single();
        entry.Accomplishments.ShouldBe("Drink Coffee");
    }
    
    [TestMethod]
    public async Task Task_WorkItemId()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);

        //Act
        var period = await UpdateDescriptionAsync(kimaiEntry, $"D#{workItem.Id}");

        //Assert
        var entry = period.TimeEntries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task Task_WorkItemType()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);

        //Act
        var period = await UpdateDescriptionAsync(kimaiEntry, $"D#{workItem.Id}");

        //Assert
        var entry = period.TimeEntries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Type.ShouldBe(WorkItemType.FromApiValue(workItem.Fields.WorkItemType));
    }
    
    [TestMethod]
    public async Task Task_ParentWorkItem()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);

        //Act
        var period = await UpdateDescriptionAsync(kimaiEntry, $"D#{workItem.Id} {workItem.Fields.Title} » D#{task.Id} {task.Fields.Title}");

        //Assert
        var entry = period.TimeEntries.Single();
        entry.Task.ShouldNotBeNull();
        entry.Task.Id.ShouldBe(task.Id);
        entry.Task.Type.ShouldBe(WorkItemType.FromApiValue(task.Fields.WorkItemType));
        entry.Task.Parent.ShouldNotBeNull();
        entry.Task.Parent.Id.ShouldBe(workItem.Id);
        entry.Task.Parent.Type.ShouldBe(WorkItemType.FromApiValue(workItem.Fields.WorkItemType));
    }
    
    [TestMethod]
    public async Task Url()
    {
        //Arrange
        var kimaiEntry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);

        //Act
        var period = await UpdateDescriptionAsync(kimaiEntry, $"D#{workItem.Id} {workItem.Fields.Title} » D#{task.Id} {task.Fields.Title}");

        //Assert
        var expected = Kimai.BaseUrl
            .AppendPathSegment(Kimai.CurrentUser.Language?.Replace("-", "_") ?? throw new InvalidOperationException())
            .AppendPathSegment("timesheet")
            .AppendQueryParam("daterange", $"{Today:O} - {Today:O}")
            .AppendQueryParam("state", 1)  //  & running
            .AppendQueryParam("billable", 0)
            .AppendQueryParam("exported", 1)
            .AppendQueryParam("orderBy", "begin")
            .AppendQueryParam("order", "DESC")
            .AppendQueryParam("searchTerm", string.Empty)
            .AppendQueryParam("performSearch", "performSearch")
            .ToUri();

        period.Days.Single().Url.ShouldBe(expected);
    }
}
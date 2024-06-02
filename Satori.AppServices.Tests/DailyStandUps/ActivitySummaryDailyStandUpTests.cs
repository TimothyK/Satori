using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.Models;
using Shouldly;
using TimeEntry = Satori.Kimai.Models.TimeEntry;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class ActivitySummaryDailyStandUpTests : DailyStandUpTests
{
    public ActivitySummaryDailyStandUpTests() : base()
    {
        ActivityUnderTest = TestActivities.SingleRandom();
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }
    #region Helpers

    #region Arrange

    private Activity ActivityUnderTest { get; }
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }


    private TimeEntry BuildTimeEntry() => BuildTimeEntry(ActivityUnderTest, Today);

    #endregion Arrange

    #region Act

    private async Task<ActivitySummary> GetActivitySummaryAsync()
    {
        var srv = new StandUpService(Kimai.AsInterface(), AzureDevOps.AsInterface());
        var days = await srv.GetStandUpDaysAsync(Today, Today);
        await srv.GetWorkItemsAsync(days);

        return days
            .Single(day => day.Date == Today)
            .Projects
            .SelectMany(p => p.Activities)
            .Single(act => act.ActivityId == ActivityUnderTest.Id);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        BuildTimeEntry();

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.ShouldNotBeNull();
        activitySummary.ActivityId.ShouldBe(ActivityUnderTest.Id);
    }

    [TestMethod]
    public async Task NoWorkItem()
    {
        //Arrange
        var entry = BuildTimeEntry();
        entry.Description = null;

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.ShouldBeEmpty();
    }
    
    [TestMethod]
    public async Task SingleWorkItem()
    {
        //Arrange
        var entry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        entry.AddWorkItems(task);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Length.ShouldBe(1);
        var taskSummary = activitySummary.TaskSummaries.Single().Task;
        taskSummary.ShouldNotBeNull();
        taskSummary.Id.ShouldBe(task.Id);
    }
    
    [TestMethod]
    public async Task SingleWorkItem_TotalTime()
    {
        //Arrange
        var entry = BuildTimeEntry();
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        entry.AddWorkItems(task);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Single().TotalTime.ShouldBe(entry.End!.Value - entry.Begin);
    }
    
    [TestMethod]
    public async Task TwoWorkItems_SeparateSummaries()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task1);
        var entry1 = BuildTimeEntry();
        entry1.AddWorkItems(task1);

        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task2);
        var entry2 = BuildTimeEntry();
        entry2.AddWorkItems(task2);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Length.ShouldBe(2);
        activitySummary.TaskSummaries.Single(x => x.Task?.Id == task1.Id).TotalTime.ShouldBe(entry1.End!.Value - entry1.Begin);
        activitySummary.TaskSummaries.Single(x => x.Task?.Id == task2.Id).TotalTime.ShouldBe(entry2.End!.Value - entry2.Begin);
    }
    
    [TestMethod]
    public async Task OneWorkItemOnTwoEntries_Summed()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        
        var entry1 = BuildTimeEntry();
        entry1.AddWorkItems(task);

        var entry2 = BuildTimeEntry();
        entry2.AddWorkItems(task);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Length.ShouldBe(1);
        var expected = new[] {entry1, entry2}.Select(x => x.End!.Value - x.Begin).Sum();
        activitySummary.TaskSummaries.Single(x => x.Task?.Id == task.Id).TotalTime.ShouldBe(expected);
    }

    [TestMethod]
    public async Task TwoEntries_OneWithTask_SeparateSummaries()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var entry1 = BuildTimeEntry();
        entry1.AddWorkItems(task);

        var entry2 = BuildTimeEntry();

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Length.ShouldBe(2);
        activitySummary.TaskSummaries.Single(x => x.Task?.Id == task.Id).TotalTime.ShouldBe(entry1.End!.Value - entry1.Begin);
        activitySummary.TaskSummaries.Single(x => x.Task == null).TotalTime.ShouldBe(entry2.End!.Value - entry2.Begin);
    }

    [TestMethod]
    public async Task TimeRemaining()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var remaining = TimeSpan.FromHours(5).Randomize().ToNearest(TimeSpan.FromMinutes(3));
        task.Fields.RemainingWork = remaining.TotalHours;

        var entry = BuildTimeEntry();
        entry.AddWorkItems(task);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Single().TimeRemaining.ShouldBe(remaining - (entry.End!.Value - entry.Begin));
    }
    
    [TestMethod]
    public async Task NeedsEstimate_Yes()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.InProgress.ToApiValue();
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;

        var entry = BuildTimeEntry();
        entry.AddWorkItems(task);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Single().NeedsEstimate.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task NeedsEstimate_No()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.State = ScrumState.Done.ToApiValue();

        var entry = BuildTimeEntry();
        entry.AddWorkItems(task);

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.Single().NeedsEstimate.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task NoTimeEntryComments()
    {
        //Arrange
        var entry = BuildTimeEntry();
        entry.Description = null;

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.TaskSummaries.ShouldBeEmpty();
        activitySummary.Accomplishments.ShouldBeNull();
        activitySummary.Impediments.ShouldBeNull();
        activitySummary.Learnings.ShouldBeNull();
        activitySummary.OtherComments.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task OtherComment()
    {
        //Arrange
        var entry = BuildTimeEntry();
        entry.Description = "Client Meeting";

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.OtherComments.ShouldBe(entry.Description);
    }
    
    [TestMethod]
    public async Task SameComment_Removed()
    {
        //Arrange
        var entry1 = BuildTimeEntry();
        entry1.Description = "Client Meeting";

        var entry2 = BuildTimeEntry();
        entry2.Description = entry1.Description;

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.OtherComments.ShouldBe(entry1.Description);
    }
    
    [TestMethod]
    public async Task DuplicateLines_Removed()
    {
        //Arrange
        var entry1 = BuildTimeEntry();
        entry1.Description = """
                             Client Meeting
                             Took first step towards TOTAL ENLIGHTENMENT
                             """;

        var entry2 = BuildTimeEntry();
        entry2.Description = """
                             Client Meeting
                             
                             Took second step towards TOTAL ENLIGHTENMENT
                             """;

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.OtherComments.ShouldBe("""
                                               Client Meeting
                                               Took first step towards TOTAL ENLIGHTENMENT
                                               Took second step towards TOTAL ENLIGHTENMENT
                                               """);
    }
    
    [TestMethod]
    public async Task Comments_TortureTest()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task1);
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task2);

        BuildTimeEntry().Description = "Client Meeting";
        BuildTimeEntry().Description = """
                                       Client Meeting
                                       🏆 Drank coffee
                                       """;
        BuildTimeEntry().Description = """
                                       🏆 Drank coffee
                                       🧱 Bathroom queues
                                       🧠 Bladder control
                                       """;

        BuildTimeEntry().Description = $"""
                                        🏆 Drank coffee
                                        D#{task1.Id}
                                        """;
        BuildTimeEntry().Description = $"""
                                        D#{task1.Id}
                                        Client Meeting
                                        🏆 Took first step towards TOTAL ENLIGHTENMENT
                                        """;
        BuildTimeEntry().Description = $"D#{task2.Id}";

        //Act
        var activitySummary = await GetActivitySummaryAsync();

        //Assert
        activitySummary.Accomplishments.ShouldBe("""
                                               Drank coffee
                                               Took first step towards TOTAL ENLIGHTENMENT
                                               """);
        activitySummary.Impediments.ShouldBe("Bathroom queues");
        activitySummary.Learnings.ShouldBe("Bladder control");
        activitySummary.OtherComments.ShouldBe("Client Meeting");
        activitySummary.TaskSummaries.Length.ShouldBe(3);
    }
}
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class GetWorkItemTests : DailyStandUpTests
{
    #region Helpers

    #region Arrange

    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    public GetWorkItemTests()
    {
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    #endregion Arrange

    #region Act

    private async Task<WorkItem?> GetWorkItemAsync(int workItemId)
    {
        var actual = await Server.GetWorkItemAsync(workItemId);
        return actual;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);

        //Act
        var actual = await GetWorkItemAsync(workItem.Id);

        //Assert
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(workItem.Id);
    }
    
    [TestMethod]
    public async Task WorkItemNotFound_Null()
    {
        //Arrange

        //Act
        var actual = await GetWorkItemAsync(42);

        //Assert
        actual.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task ParentIsLoaded()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem).AddChild(out var task);

        //Act
        var actual = await GetWorkItemAsync(task.Id);

        //Assert
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(task.Id);
        actual.Parent.ShouldNotBeNull();
        actual.Parent.Id.ShouldBe(workItem.Id);
        actual.Parent.Type.ShouldBe(WorkItemType.FromApiValue(workItem.Fields.WorkItemType));
    }
    
    [TestMethod]
    public async Task ChildrenAreLoaded()
    {
        //Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem)
            .AddChild(out var task1)
            .AddChild(out var task2);

        //Act
        var actual = await GetWorkItemAsync(workItem.Id);

        //Assert
        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(workItem.Id);
        actual.Parent.ShouldBeNull();
        actual.Children.ShouldNotBeEmpty();
        actual.Children.Count.ShouldBe(2);
        actual.Children.SingleOrDefault(t => t.Id == task1.Id).ShouldNotBeNull();
        actual.Children.SingleOrDefault(t => t.Id == task2.Id).ShouldNotBeNull();
    }
}
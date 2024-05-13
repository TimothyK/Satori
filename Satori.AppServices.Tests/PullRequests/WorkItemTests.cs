using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.Builders;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.PullRequests;

/// <summary>
/// Tests the WorkItem view model that is returned from <seealso cref="PullRequestService.GetPullRequestsAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The WorkItem view model returned from here has fewer properties populated then if the WorkItem was read from the WorkItemService.
/// The WorkItemService give more details related to the child tasks and parents, and the pull requests belongs to the work item.
/// This is simply the work items directly linked to the PR.
/// </para>
/// </remarks>
[TestClass]
public class WorkItemTests
{
    private readonly TestAzureDevOpsServer _azureDevOpsServer;
    private readonly AzureDevOpsDatabaseBuilder _builder;
    private Uri AzureDevOpsRootUrl => _azureDevOpsServer.AsInterface().ConnectionSettings.Url;

    public WorkItemTests()
    {
        _azureDevOpsServer = new TestAzureDevOpsServer();
        _builder = _azureDevOpsServer.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    private WorkItem? _expected;
    private WorkItem Expected
    {
        get
        {
            if (_expected != null)
            {
                return _expected;
            }

            _builder.BuildPullRequest().WithWorkItem(out _expected);
            return _expected;
        }
    }

    #endregion Arrange

    #region Act

    private IEnumerable<ViewModels.WorkItems.WorkItem> GetWorkItems()
    {
        var srv = new PullRequestService(_azureDevOpsServer.AsInterface(), NullLoggerFactory.Instance);
        var pullRequests = srv.GetPullRequestsAsync().Result.ToArray();
        srv.AddWorkItemsToPullRequestsAsync(pullRequests).GetAwaiter().GetResult();
        return [.. pullRequests.Single().WorkItems];
    }

    private ViewModels.WorkItems.WorkItem GetSingleWorkItem()
    {
        _ = Expected;  //Force lazy load of expected WorkItem

        return GetWorkItems().Single();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        var expected = Expected;

        //Act
        var actual = GetSingleWorkItem();

        //Assert
        actual.Id.ShouldBe(expected.Id);
    }

    [TestMethod] public void Title() => GetSingleWorkItem().Title.ShouldBe(Expected.Fields.Title);
    
    [TestMethod] 
    public void AssignedTo()
    {
        var expected = Expected.Fields.AssignedTo ?? throw new InvalidOperationException();

        var actual = GetSingleWorkItem().AssignedTo;

        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ShouldBe(expected.ImageUrl);
    }
    
    [TestMethod] 
    public void CreatedBy()
    {
        var expected = Expected.Fields.CreatedBy;

        var actual = GetSingleWorkItem().CreatedBy;

        actual.ShouldNotBeNull();
        actual.Id.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ShouldBe(expected.ImageUrl);
    }

    [TestMethod] public void CreatedDate() => GetSingleWorkItem().CreatedDate.ShouldBe(Expected.Fields.SystemCreatedDate);

    [TestMethod] public void IterationPath() => GetSingleWorkItem().IterationPath.ShouldBe(Expected.Fields.IterationPath);

    
    [TestMethod]
    [DataRow("Bug")]
    [DataRow("Product Backlog Item")]
    [DataRow("Task")]
    [DataRow("Feature")]
    [DataRow("Epic")]
    [DataRow("garbage")]
    public void Type(string type)
    {
        //Arrange
        var workItem = Expected;
        workItem.Fields.WorkItemType = type;
        var expected = WorkItemType.FromApiValue(type);

        //Act
        var actual = GetSingleWorkItem();

        //Assert
        actual.Type.ShouldBe(expected);
    }


    [TestMethod] public void State() => GetSingleWorkItem().State.ToApiValue().ShouldBe(Expected.Fields.State);
    
    [TestMethod] public void ProjectCode() => GetSingleWorkItem().ProjectCode.ShouldBe(Expected.Fields.ProjectCode);

    [TestMethod] public void Url() => GetSingleWorkItem().Url.ShouldBe(AzureDevOpsRootUrl + "/_workItems/edit/" + Expected.Id);
}
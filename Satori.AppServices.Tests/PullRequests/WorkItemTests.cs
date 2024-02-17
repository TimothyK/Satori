using Moq;
using Pscl.Linq;
using Satori.AppServices.Services;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
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
    #region Helpers

    #region Arrange

    private const string AzureDevOpsRootUrl = "http://azuredevops.test/Team";

    private readonly List<WorkItem> _workItems = new();

    private WorkItem Expected => _workItems.SingleOrDefault() ?? BuildWorkItem();

    private WorkItem BuildWorkItem()
    {
        var workItem = Builder.Builder<WorkItem>.New().Build(int.MaxValue);
        _workItems.Add(workItem);
        return workItem;
    }

    private static PullRequest BuildPullRequest()
    {
        var pr = Builder.Builder<PullRequest>.New().Build(int.MaxValue);
        pr.Reviewers = Array.Empty<Reviewer>();
        return pr;
    }

    #endregion Arrange

    #region Act

    private ViewModels.WorkItems.WorkItem[] GetWorkItems(params int[] workItemIds)
    {
        //Arrange
        var pr = BuildPullRequest();
        var mock = new Mock<IAzureDevOpsServer>();
        mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings() { Url = new Uri(AzureDevOpsRootUrl), PersonalAccessToken = "token" });

        mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(pr.Yield().ToArray());

        mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(pr))
            .ReturnsAsync(GetWorkItemMap);
        IdMap[] GetWorkItemMap()
        {
            return workItemIds
                .Select(id => Builder.Builder<IdMap>.New().Build(idMap => idMap.Id = id))
                .ToArray();
        }

        mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> ids) => _workItems.Where(wi => wi.Id.IsIn(ids)).ToArray());

        //Act
        var srv = new PullRequestService(mock.Object);
        return srv.GetPullRequestsAsync().Result.Single().WorkItems.ToArray();
    }

    private ViewModels.WorkItems.WorkItem GetSingleWorkItem()
    {
        if (_workItems.Count > 1)
        {
            throw new InvalidOperationException("Only arrange 1 work item");
        }
        if (_workItems.None())
        {
            BuildWorkItem();
        }

        var id = _workItems.Single().Id;
        return GetWorkItems(id).Single();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void SmokeTest()
    {
        //Arrange
        var expected = BuildWorkItem();

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
        var workItem = BuildWorkItem();
        workItem.Fields.WorkItemType = type;
        var expected = WorkItemType.FromApiValue(type);

        //Act
        var actual = GetSingleWorkItem();

        //Assert
        actual.Type.ShouldBe(expected);
    }

    [TestMethod] public void State() => GetSingleWorkItem().State.ShouldBe(Expected.Fields.State);
    
    [TestMethod] public void ProjectCode() => GetSingleWorkItem().ProjectCode.ShouldBe(Expected.Fields.ProjectCode);

    [TestMethod] public void Url() => GetSingleWorkItem().Url.ShouldBe(AzureDevOpsRootUrl + "/_workItems/edit/" + Expected.Id);
}
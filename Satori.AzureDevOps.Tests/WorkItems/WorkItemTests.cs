using Autofac;
using Flurl;
using Pscl.CommaSeparatedValues;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.WorkItems;

[TestClass]
public class WorkItemTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetWorkItemUrl(params int[] workItemIds) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendQueryParam("ids", workItemIds.ToCommaSeparatedValues())
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, byte[] response)
    {
        _mockHttp.When(url).Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private static WorkItem[] GetWorkItems(int workItemId)
    {
        var srv = Globals.Services.Scope.Resolve<IAzureDevOpsServer>();
        return srv.GetWorkItemsAsync(workItemId).Result;
    }

    private WorkItem SingleWorkItem(int workItemId = SingleWorkItemId)
    {
        //Arrange
        SetResponse(GetWorkItemUrl(workItemId), GetPayload(workItemId));

        //Act
        var workItems = GetWorkItems(workItemId);

        //Assert
        workItems.Length.ShouldBe(1);
        return workItems.Single();
    }

    private const int SingleWorkItemId = 2;
    private const int BlockedWorkItemId = 3;

    private static byte[] GetPayload(int workItemId)
    {
        if (workItemId == BlockedWorkItemId)
        {
            return WorkItemResponses.BlockedWorkItem;
        }
        return WorkItemResponses.SingleWorkItem;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest() => SingleWorkItem().Id.ShouldBe(SingleWorkItemId);

    [TestMethod]
    public void Title() => SingleWorkItem().Fields.Title.ShouldBe("Program no longer crashes on startup");

    [TestMethod]
    public void WorkItemType() => SingleWorkItem().Fields.WorkItemType.ShouldBe("Product Backlog Item");


    [TestMethod]
    public void Area() => SingleWorkItem().Fields.AreaPath.ShouldBe("Product\\AppArea");

    [TestMethod]
    public void IterationPath() => SingleWorkItem().Fields.IterationPath.ShouldBe("CD\\Skunk\\Sprint 2024-02");

    [TestMethod]
    public void State() => SingleWorkItem().Fields.State.ShouldBe("New");

    [TestMethod]
    public void AssignedTo()
    {
        var assignedTo = SingleWorkItem().Fields.AssignedTo;
        assignedTo.ShouldNotBeNull();
        assignedTo.Id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
        assignedTo.DisplayName.ShouldBe("Timothy Klenke");
        assignedTo.ImageUrl.ShouldBe("http://devops.test/Org/_apis/GraphProfile/MemberAvatars/win.Uy0xLTUtMjEtMTUyNzAwNjgzMS04OTQzOTEwNDQtNjIyNjExMjE2LTExNjc");
    }
        
    [TestMethod]
    public void CreatedBy()
    {
        var createdBy = SingleWorkItem().Fields.CreatedBy;
        createdBy.ShouldNotBeNull();
        createdBy.Id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
        createdBy.DisplayName.ShouldBe("Timothy Klenke");
        createdBy.ImageUrl.ShouldBe("http://devops.test/Org/_apis/GraphProfile/MemberAvatars/win.Uy0xLTUtMjEtMTUyNzAwNjgzMS04OTQzOTEwNDQtNjIyNjExMjE2LTExNjc");
    }

    [TestMethod]
    public void CreationDate() => SingleWorkItem().Fields.SystemCreatedDate
        .ShouldBe(new DateTimeOffset(2024, 1, 13, 20, 16, 53, TimeSpan.Zero).AddMilliseconds(407));


    [TestMethod]
    public void Priority() => SingleWorkItem().Fields.Priority.ShouldBe(2);

    [TestMethod]
    public void BacklogPriority() => SingleWorkItem().Fields.BacklogPriority.ShouldBe(27103990.0);

    [TestMethod]
    public void Blocked_No() => SingleWorkItem().Fields.Blocked.ShouldBeFalse();
        
    [TestMethod]
    public void Blocked_Yes() => SingleWorkItem(BlockedWorkItemId).Fields.Blocked.ShouldBeTrue();

    [TestMethod]
    public void ProjectCode() => SingleWorkItem().Fields.ProjectCode.ShouldBe("1.2.3 - Skunk Works");
        
    [TestMethod]
    public void CommentCount() => SingleWorkItem().Fields.CommentCount.ShouldBe(0);

}
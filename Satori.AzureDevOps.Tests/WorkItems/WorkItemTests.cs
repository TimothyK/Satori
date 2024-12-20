﻿using Autofac;
using Flurl;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.WorkItems;

[TestClass]
public class WorkItemTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Services.Scope.Resolve<ConnectionSettings>();

    private Url GetWorkItemUrl(params int[] workItemIds) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendQueryParam("ids", string.Join(',', workItemIds))
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp = Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    #endregion Arrange

    #region Act

    private static WorkItem[] GetWorkItems(int workItemId)
    {
        var srv = Services.Scope.Resolve<IAzureDevOpsServer>();
        return srv.GetWorkItemsAsync(workItemId).Result;
    }

    private WorkItem SingleWorkItem(int workItemId = SingleWorkItemId)
    {
        //Arrange
        SetResponse(GetWorkItemUrl(workItemId), GetPayload(workItemId));

        //Act
        var workItems = GetWorkItems(workItemId);

        //Assert
        return workItems.Single(wi => wi.Id == workItemId);
    }

    private const int SingleWorkItemId = 2;
    private const int BlockedWorkItemId = 3;
    private const int ExpandedWorkItemId = 12345;
    private static readonly int[] SixTypesWorkItemIds = [28655, 29924, 29922, 29923, 27850, 30343];

    private static string GetPayload(int workItemId)
    {
        if (workItemId == BlockedWorkItemId)
        {
            return WorkItemResponses.BlockedWorkItem;
        }    
        if (workItemId == ExpandedWorkItemId)
        {
            return WorkItemResponses.ExpandedRelations;
        }
        if (SixTypesWorkItemIds.Contains(workItemId))
        {
            return WorkItemResponses.SixTypesWorkItems;
        }
        return WorkItemResponses.SingleWorkItem;
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest() => SingleWorkItem().Id.ShouldBe(SingleWorkItemId);

    [TestMethod]
    public void AzureDisabled_ThrowsInvalidOp()
    {
        //Arrange
        var factory = Services.Scope.Resolve<ConnectionSettingsFactory>();
        var connectionSettings = new ConnectionSettings()
        {
            Enabled = false,
            Url = new Uri("http://devops.test/Org"),
            PersonalAccessToken = "test"
        };

        //Act
        AggregateException ex;
        using (factory.Set(connectionSettings))
        {
            ex = Should.Throw<AggregateException>(() => SingleWorkItem());
        }

        //Assert
        ex.InnerExceptions.Count.ShouldBe(1);
        var innerException = ex.InnerExceptions.Single();
        innerException.ShouldBeOfType<InvalidOperationException>();
        innerException.Message.ShouldBe("Azure DevOps is not enabled.  Check settings on Home page.");
    }

    [TestMethod]
    public void NoWorkItems_DoesNotCallWebApi()
    {
        //Arrange
        _mockHttp.Fallback.Throw(new Exception("Should not call the web API"));

        //Act
        var srv = Services.Scope.Resolve<IAzureDevOpsServer>();
        var workItems = srv.GetWorkItemsAsync().Result;

        //Assert
        workItems.ShouldBeEmpty();
    }

    [TestMethod]
    public void Title() => SingleWorkItem().Fields.Title.ShouldBe("Program no longer crashes on startup");

    [TestMethod]
    public void Revision() => SingleWorkItem().Rev.ShouldBe(16);

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

    [TestMethod]
    [DataRow(28655, "Epic")]
    [DataRow(29924, "Feature")]
    [DataRow(29922, "Product Backlog Item")]
    [DataRow(29923, "Task")]
    [DataRow(27850, "Bug")]
    [DataRow(30343, "Impediment")]
    public void Type(int workItemId, string type) => SingleWorkItem(workItemId).Fields.WorkItemType.ShouldBe(type);

    /// <summary>
    /// Original Estimate is only available on Task work items
    /// </summary>
    [TestMethod]
    public void OriginalEstimate_Missing() => SingleWorkItem().Fields.OriginalEstimate.ShouldBeNull();
    /// <summary>
    /// Completed Work is only available on Task work items
    /// </summary>
    [TestMethod]
    public void CompletedWork_Missing() => SingleWorkItem().Fields.CompletedWork.ShouldBeNull();
    /// <summary>
    /// Remaining Work is only available on Task work items
    /// </summary>
    [TestMethod]
    public void RemainingWork_Missing() => SingleWorkItem().Fields.RemainingWork.ShouldBeNull();
    
    [TestMethod]
    public void OriginalEstimate() => SingleWorkItem(29923).Fields.OriginalEstimate.ShouldBe(12.0);
    [TestMethod]
    public void CompletedWork() => SingleWorkItem(29923).Fields.CompletedWork.ShouldBe(9.0);
    [TestMethod]
    public void RemainingWork() => SingleWorkItem(29923).Fields.RemainingWork.ShouldBe(3.0);

    [TestMethod]
    public void Tags() => SingleWorkItem().Fields.Tags.ShouldBe("Needs_Design_Review; Waiting_For_Client");
    
    [TestMethod]
    public void Triage() => SingleWorkItem(27850).Fields.Triage.ShouldBe("Pending");

    [TestMethod]
    public void Parent() => SingleWorkItem(ExpandedWorkItemId).Fields.Parent.ShouldBe(12344);

    
    [TestMethod]
    public void ParentViaRelations()
    {
        //Act
        var workItem = SingleWorkItem(ExpandedWorkItemId);

        //Assert
        workItem.Relations.ShouldNotBeNull();
        var parentRelation = workItem.Relations.SingleOrDefault(r => r.RelationType == "System.LinkTypes.Hierarchy-Reverse");
        parentRelation.ShouldNotBeNull();
        parentRelation.Url.ShouldEndWith("/12344");
        parentRelation.Attributes["name"].ToString().ShouldBe("Parent");
    }
}
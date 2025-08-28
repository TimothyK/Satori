using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.WorkItems.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.WorkItems;

[TestClass]
public class WorkItemTests
{
    private readonly ServiceProvider _serviceProvider;

    public WorkItemTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
    }

    #region Helpers

    #region Arrange

    private ConnectionSettings ConnectionSettings => _serviceProvider.GetRequiredService<ConnectionSettings>();

    private Url GetWorkItemUrl(params int[] workItemIds) =>
        ConnectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendQueryParam("ids", string.Join(',', workItemIds))
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    #endregion Arrange

    #region Act

    private async Task<WorkItem> SingleWorkItemAsync(int workItemId = SingleWorkItemId)
    {
        //Arrange
        SetResponse(GetWorkItemUrl(workItemId), GetPayload(workItemId));

        //Act
        var workItems = await GetWorkItemsAsync(workItemId);

        //Assert
        return workItems.Single(wi => wi.Id == workItemId);
    }

    private async Task<WorkItem[]> GetWorkItemsAsync(int workItemId)
    {
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return await srv.GetWorkItemsAsync(workItemId);
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
    public async Task ASmokeTest() => (await SingleWorkItemAsync()).Id.ShouldBe(SingleWorkItemId);

    [TestMethod]
    public async Task AzureDisabled_ThrowsInvalidOp()
    {
        //Arrange
        var factory = _serviceProvider.GetRequiredService<ConnectionSettingsFactory>();
        var connectionSettings = new ConnectionSettings()
        {
            Enabled = false,
            Url = new Uri("http://devops.test/Org"),
            PersonalAccessToken = "test"
        };

        //Act
        InvalidOperationException ex;
        using (factory.Set(connectionSettings))
        {
            ex = await Should.ThrowAsync<InvalidOperationException>(async () => await SingleWorkItemAsync());
        }

        //Assert
        ex.Message.ShouldBe("Azure DevOps is not enabled.  Check settings on Home page.");
    }

    [TestMethod]
    public void NoWorkItems_DoesNotCallWebApi()
    {
        //Arrange
        _mockHttp.Fallback.Throw(new Exception("Should not call the web API"));

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var workItems = srv.GetWorkItemsAsync().Result;

        //Assert
        workItems.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Title() => (await SingleWorkItemAsync()).Fields.Title.ShouldBe("Program no longer crashes on startup");

    [TestMethod]
    public async Task Revision() => (await SingleWorkItemAsync()).Rev.ShouldBe(16);

    [TestMethod]
    public async Task WorkItemType() => (await SingleWorkItemAsync()).Fields.WorkItemType.ShouldBe("Product Backlog Item");


    [TestMethod]
    public async Task Area() => (await SingleWorkItemAsync()).Fields.AreaPath.ShouldBe("Product\\AppArea");

    [TestMethod]
    public async Task IterationPath() => (await SingleWorkItemAsync()).Fields.IterationPath.ShouldBe("CD\\Skunk\\Sprint 2024-02");

    [TestMethod]
    public async Task State() => (await SingleWorkItemAsync()).Fields.State.ShouldBe("New");

    [TestMethod]
    public async Task AssignedTo()
    {
        var assignedTo = (await SingleWorkItemAsync()).Fields.AssignedTo;
        assignedTo.ShouldNotBeNull();
        assignedTo.Id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
        assignedTo.DisplayName.ShouldBe("Timothy Klenke");
        assignedTo.ImageUrl.ShouldBe("http://devops.test/Org/_apis/GraphProfile/MemberAvatars/win.Uy0xLTUtMjEtMTUyNzAwNjgzMS04OTQzOTEwNDQtNjIyNjExMjE2LTExNjc");
    }
        
    [TestMethod]
    public async Task CreatedBy()
    {
        var createdBy = (await SingleWorkItemAsync()).Fields.CreatedBy;
        createdBy.ShouldNotBeNull();
        createdBy.Id.ShouldBe(new Guid("c00ef764-dc77-4b32-9a19-590db59f039b"));
        createdBy.DisplayName.ShouldBe("Timothy Klenke");
        createdBy.ImageUrl.ShouldBe("http://devops.test/Org/_apis/GraphProfile/MemberAvatars/win.Uy0xLTUtMjEtMTUyNzAwNjgzMS04OTQzOTEwNDQtNjIyNjExMjE2LTExNjc");
    }

    [TestMethod]
    public async Task CreationDate() => (await SingleWorkItemAsync()).Fields.SystemCreatedDate
        .ShouldBe(new DateTimeOffset(2024, 1, 13, 20, 16, 53, TimeSpan.Zero).AddMilliseconds(407));


    [TestMethod]
    public async Task Priority() => (await SingleWorkItemAsync()).Fields.Priority.ShouldBe(2);

    [TestMethod]
    public async Task TargetDate() => (await SingleWorkItemAsync()).Fields.TargetDate
        .ShouldBe(new DateTimeOffset(2025, 4, 14, 6, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public async Task BacklogPriority() => (await SingleWorkItemAsync()).Fields.BacklogPriority.ShouldBe(27103990.0);

    [TestMethod]
    public async Task Blocked_No() => (await SingleWorkItemAsync()).Fields.Blocked.ShouldBeFalse();
        
    [TestMethod]
    public async Task Blocked_Yes() => (await SingleWorkItemAsync(BlockedWorkItemId)).Fields.Blocked.ShouldBeTrue();

    [TestMethod]
    public async Task ProjectCode() => (await SingleWorkItemAsync()).Fields.ProjectCode.ShouldBe("1.2.3 - Skunk Works");
        
    [TestMethod]
    public async Task CommentCount() => (await SingleWorkItemAsync()).Fields.CommentCount.ShouldBe(0);

    [TestMethod]
    [DataRow(28655, "Epic")]
    [DataRow(29924, "Feature")]
    [DataRow(29922, "Product Backlog Item")]
    [DataRow(29923, "Task")]
    [DataRow(27850, "Bug")]
    [DataRow(30343, "Impediment")]
    public async Task Type(int workItemId, string type) => (await SingleWorkItemAsync(workItemId)).Fields.WorkItemType.ShouldBe(type);

    /// <summary>
    /// Original Estimate is only available on Task work items
    /// </summary>
    [TestMethod]
    public async Task OriginalEstimate_Missing() => (await SingleWorkItemAsync()).Fields.OriginalEstimate.ShouldBeNull();
    /// <summary>
    /// Completed Work is only available on Task work items
    /// </summary>
    [TestMethod]
    public async Task CompletedWork_Missing() => (await SingleWorkItemAsync()).Fields.CompletedWork.ShouldBeNull();
    /// <summary>
    /// Remaining Work is only available on Task work items
    /// </summary>
    [TestMethod]
    public async Task RemainingWork_Missing() => (await SingleWorkItemAsync()).Fields.RemainingWork.ShouldBeNull();
    
    [TestMethod]
    public async Task OriginalEstimate() => (await SingleWorkItemAsync(29923)).Fields.OriginalEstimate.ShouldBe(12.0);
    [TestMethod]
    public async Task CompletedWork() => (await SingleWorkItemAsync(29923)).Fields.CompletedWork.ShouldBe(9.0);
    [TestMethod]
    public async Task RemainingWork() => (await SingleWorkItemAsync(29923)).Fields.RemainingWork.ShouldBe(3.0);

    [TestMethod]
    public async Task Tags() => (await SingleWorkItemAsync()).Fields.Tags.ShouldBe("Needs_Design_Review; Waiting_For_Client");
    
    [TestMethod]
    public async Task Triage() => (await SingleWorkItemAsync(27850)).Fields.Triage.ShouldBe("Pending");

    [TestMethod]
    public async Task Parent() => (await SingleWorkItemAsync(ExpandedWorkItemId)).Fields.Parent.ShouldBe(12344);

    
    [TestMethod]
    public async Task ParentViaRelations()
    {
        //Act
        var workItem = await SingleWorkItemAsync(ExpandedWorkItemId);

        //Assert
        workItem.Relations.ShouldNotBeNull();
        var parentRelation = workItem.Relations.SingleOrDefault(r => r.RelationType == "System.LinkTypes.Hierarchy-Reverse");
        parentRelation.ShouldNotBeNull();
        parentRelation.Url.ShouldEndWith("/12344");
        parentRelation.Attributes["name"].ToString().ShouldBe("Parent");
    }
}
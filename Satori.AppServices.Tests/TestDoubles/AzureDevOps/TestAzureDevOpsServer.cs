using CodeMonkeyProjectiles.Linq;
using Moq;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Database;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Shouldly;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps;

/// <summary>
/// Mock implementation of <see cref="IAzureDevOpsServer"/>
/// </summary>
/// <remarks>
/// <para>
/// This class doesn't implement <see cref="IAzureDevOpsServer"/>, but it can be used as that via <see cref="AsInterface"/>.
/// </para>
/// <para>
/// This class does hold a database of objects that will be returned from <see cref="IAzureDevOpsServer"/>.
/// This database is internal, but can be written to via <see cref="CreateBuilder"/>.
/// </para>
/// </remarks>
internal class TestAzureDevOpsServer
{
    private const string AzureDevOpsRootUrl = "http://devops.test/Org";

    #region Database

    private readonly AzureDevOpsDatabase _database = new();
    public AzureDevOpsDatabaseBuilder CreateBuilder() => new(_database);

    #endregion Database

    private readonly Mock<IAzureDevOpsServer> _mock;

    public TestAzureDevOpsServer()
    {
        TestUserAzureDevOpsId = Guid.NewGuid();
        Identity = new Identity
        {
            Id = TestUserAzureDevOpsId,
            ProviderDisplayName = "Test User (AzDO)",
            Properties = new IdentityProperties()
            {
                Description = new IdentityPropertyValue<string>() { Value = "Code Monkey" },
                Domain = new IdentityPropertyValue<string>() { Value = "DomainName" },
                Account = new IdentityPropertyValue<string>() { Value = "TimothyK" },
                Mail = new IdentityPropertyValue<string>() { Value = "timothy@klenkeverse.com" },
            }
        };

        _mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);
        _mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings { Url = new Uri(AzureDevOpsRootUrl), PersonalAccessToken = "token" });

        _mock.Setup(srv => srv.GetCurrentUserIdAsync())
            .ReturnsAsync(() => TestUserAzureDevOpsId);

        _mock.Setup(srv => srv.GetIdentityAsync(TestUserAzureDevOpsId))
            .ReturnsAsync(() => Identity);

        _mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(() => _database.GetPullRequests());

        _mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(It.IsAny<PullRequestId>()))
            .ReturnsAsync((PullRequestId pr) => GetWorkItemMap(pr));

        _mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync((IEnumerable<int> workItemIds) => GetWorkItems(workItemIds));
        _mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<int[]>()))
            .ReturnsAsync((int[] workItemIds) => GetWorkItems(workItemIds));

        _mock.Setup(srv => srv.GetTeamsAsync())
            .ReturnsAsync(() => _database.GetTeams());

        _mock.Setup(srv => srv.GetCurrentIterationAsync(It.IsAny<Team>()))
            .ReturnsAsync((Team team) => _database.GetIterationForTeam(team));

        _mock.Setup(srv => srv.GetIterationWorkItemsAsync(It.IsAny<IterationId>()))
            .ReturnsAsync((IterationId iteration) => _database.GetWorkItemsForIteration(iteration));

        _mock.Setup(srv => srv.PatchWorkItemAsync(It.IsAny<int>(), It.IsAny<IEnumerable<WorkItemPatchItem>>()))
            .ReturnsAsync((int id, IEnumerable<WorkItemPatchItem> items) => PatchWorkItems(id, items));

        return;
        IdMap[] GetWorkItemMap(PullRequestId pullRequest)
        {
            return _database.GetWorkItemIdsForPullRequestId(pullRequest.Id)
                .Select(workItemId => Builder.Builder<IdMap>.New().Build(idMap => idMap.Id = workItemId))
                .ToArray();
        }

        WorkItem[] GetWorkItems(IEnumerable<int> workItemIds)
        {
            return _database.GetWorkItemsById(workItemIds).ToArray();
        }
    }

    public Guid TestUserAzureDevOpsId { get; }

    public Identity Identity { get; set; }


    private WorkItem PatchWorkItems(int id, IEnumerable<WorkItemPatchItem> items)
    {
        var workItem = _database.GetWorkItemsById(id.Yield()).SingleOrDefault()
            ?? throw new InvalidOperationException($"Work Item {id} not found");

        var original = JsonSerializer.Serialize(workItem);

        var revWasTested = false;

        foreach (var item in items)
        {
            if (item is { Operation: Operation.Test, Path: "/rev" })
            {
                workItem.Rev.ShouldBe(item.Value);
                revWasTested = true;
            }
            else
            {
                PatchWorkItem(workItem, item);
            }
        }

        revWasTested.ShouldBeTrue();
        if (original != JsonSerializer.Serialize(workItem))
        {
            workItem.Rev++;
        }

        return workItem;
    }

    private static void PatchWorkItem(WorkItem workItem, WorkItemPatchItem item)
    {
        if (!item.Path.StartsWith("/fields/"))
        {
            throw new NotSupportedException($"Unsupported path {item.Path}");
        }

        var propertyName = item.Path["/fields/".Length..];
        var property = workItem.Fields.GetType().GetProperties()
            .SingleOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == propertyName)
            ?? throw new InvalidOperationException($"Unknown path {item.Path}");

        property.SetValue(workItem.Fields, item.Value);
    }

    public IAzureDevOpsServer AsInterface() => _mock.Object;


}
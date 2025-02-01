using CodeMonkeyProjectiles.Linq;
using Moq;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Database;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Shouldly;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

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

    public readonly Mock<IAzureDevOpsServer> Mock;

    public TestAzureDevOpsServer()
    {
        TestUserAzureDevOpsId = Person.Me?.AzureDevOpsId ?? Guid.NewGuid();
        Identity = new Identity
        {
            Id = TestUserAzureDevOpsId,
            ProviderDisplayName = Person.Me?.DisplayName ?? "Test User (AzDO)",
            Properties = new IdentityProperties()
            {
                Description = new IdentityPropertyValue<string>() { Value = "Code Monkey" },
                Domain = new IdentityPropertyValue<string>() { Value = "DomainName" },
                Account = new IdentityPropertyValue<string>() { Value = "TimothyK" },
                Mail = new IdentityPropertyValue<string>() { Value = "timothy@klenkeverse.com" },
            }
        };

        Mock = new Mock<IAzureDevOpsServer>(MockBehavior.Strict);

        Mock.Setup(srv => srv.Enabled)
            .Returns(() => Enabled);

        Mock.Setup(srv => srv.ConnectionSettings)
            .Returns(new ConnectionSettings { Url = new Uri(AzureDevOpsRootUrl), PersonalAccessToken = "token" });

        Mock.Setup(srv => srv.GetCurrentUserIdAsync())
            .ReturnsAsync(() => TestUserAzureDevOpsId);

        Mock.Setup(srv => srv.GetIdentityAsync(TestUserAzureDevOpsId))
            .ReturnsAsync(() => Identity);

        Mock.Setup(srv => srv.GetPullRequestsAsync())
            .ReturnsAsync(() => _database.GetPullRequests());

        Mock.Setup(srv => srv.GetPullRequestWorkItemIdsAsync(It.IsAny<PullRequestId>()))
            .ReturnsAsync((PullRequestId pr) => GetWorkItemMap(pr));

        Mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>()))
            .Callback((IEnumerable<int> workItemIds) => CallOnGetWorkItems(workItemIds))
            .ReturnsAsync((IEnumerable<int> workItemIds) => GetWorkItems(workItemIds));
        Mock.Setup(srv => srv.GetWorkItemsAsync(It.IsAny<int[]>()))
            .Callback((int[] workItemIds) => CallOnGetWorkItems(workItemIds))
            .ReturnsAsync((int[] workItemIds) => GetWorkItems(workItemIds));

        Mock.Setup(srv => srv.GetTeamsAsync())
            .ReturnsAsync(() => _database.GetTeams());

        Mock.Setup(srv => srv.GetCurrentIterationAsync(It.IsAny<Team>()))
            .ReturnsAsync((Team team) => _database.GetIterationForTeam(team));

        Mock.Setup(srv => srv.GetIterationWorkItemsAsync(It.IsAny<IterationId>()))
            .ReturnsAsync((IterationId iteration) => _database.GetWorkItemsForIteration(iteration));

        Mock.Setup(srv => srv.PatchWorkItemAsync(It.IsAny<int>(), It.IsAny<IEnumerable<WorkItemPatchItem>>()))
            .ReturnsAsync((int id, IEnumerable<WorkItemPatchItem> items) => PatchWorkItems(id, items));

        Mock.Setup(srv => srv.PostWorkItemAsync(It.IsAny<string>(), It.IsAny<IEnumerable<WorkItemPatchItem>>()))
            .ReturnsAsync((string projectName, IEnumerable<WorkItemPatchItem> items) => PostWorkItems(projectName, items));

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

    private void CallOnGetWorkItems(IEnumerable<int> workItemIds)
    {
        OnGetWorkItems?.Invoke(workItemIds);
    }

    public Action<IEnumerable<int>>? OnGetWorkItems { get; set; }

    public bool Enabled { get; set; } = true;

    private Guid TestUserAzureDevOpsId { get; }

    public Identity Identity { get; set; }

    private WorkItem PostWorkItems(string projectName, IEnumerable<WorkItemPatchItem> postItems)
    {
        var builder = new AzureDevOpsDatabaseBuilder(_database);
        builder.BuildWorkItem(out var workItem);
        workItem.Fields.ProjectName = projectName;
        workItem.Rev = 1;

        var patchItems = postItems.ToList();
        patchItems.Add(new WorkItemPatchItem
        {
            Operation = Operation.Test, 
            Path = "/rev", 
            Value = workItem.Rev
        });

        return PatchWorkItems(workItem.Id, patchItems);
    }

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

    private void PatchWorkItem(WorkItem workItem, WorkItemPatchItem item)
    {
        try
        {
            if (item.Path.StartsWith("/fields/"))
            {
                PatchField(workItem, item);
            }
            else if (item.Path == "/relations/-")
            {
                PatchRelation(workItem, item);
            }
            else
            {
                throw new NotSupportedException($"Unsupported path {item.Path}");
            }
        }
        catch (Exception ex)
        {
            throw new NotSupportedException($"D#{workItem.Id} cannot have its {item.Path} property set to {item.Value}.  {ex.Message}", ex);
        }
    }

    private static void PatchField(WorkItem workItem, WorkItemPatchItem item)
    {
        if (!item.Path.StartsWith("/fields/"))
        {
            throw new NotSupportedException($"Unsupported path {item.Path}");
        }

        var propertyName = item.Path["/fields/".Length..];
        var property = workItem.Fields.GetType().GetProperties()
            .SingleOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == propertyName)
            ?? throw new InvalidOperationException($"Unknown path {item.Path}");

        if (property.PropertyType == typeof(User))
        {
            var user = GetUser(item.Value.ToString());
            property.SetValue(workItem.Fields, user);
            return;
        }

        property.SetValue(workItem.Fields, item.Value);
    }

    private static User? GetUser(string? userDisplayName)
    {
        if (string.IsNullOrEmpty(userDisplayName))
        {
            return null;
        }

        var person = Person.FromDisplayName(userDisplayName);
        if (person == null)
        {
            throw new InvalidOperationException($"User {userDisplayName} is unknown");
        }

        var user = new User()
        {
            DisplayName = person.DisplayName,
            Id = person.AzureDevOpsId,
            ImageUrl = person.AvatarUrl.ToString(),
            UniqueName = person.DomainLogin ?? string.Empty,
            Url = "https://azureDevOps.test/Org/Id?id=" + person.AzureDevOpsId,
        };
        return user;
    }

    private void PatchRelation(WorkItem workItem, WorkItemPatchItem item)
    {
        var relationEnvelope =(Dictionary<string, object>) item.Value;

        var linkType = LinkType.FromApiValue(relationEnvelope["rel"].ToString() ?? throw new InvalidOperationException("rel unknown"));
        
        var relatedWorkItemUrl = new Uri(relationEnvelope["url"].ToString() ?? throw new InvalidOperationException("url unknown"));
        var relatedWorkItem = GetWorkItemFromUrl(relatedWorkItemUrl);

        _database.AddWorkItemLink(workItem, linkType, relatedWorkItem);
    }

    private WorkItem GetWorkItemFromUrl(Uri relatedWorkItemUrl)
    {
        var relatedWorkItemId = GetLastPathSegment(relatedWorkItemUrl);

        if (!int.TryParse(relatedWorkItemId, out var id))
        {
            throw new InvalidOperationException("Url does not end in an integer");
        }

        return _database.GetWorkItemsById(id.Yield()).SingleOrDefault() 
               ?? throw new InvalidOperationException("Related work item ID not found");
    }

    public static string GetLastPathSegment(Uri uri)
    {
        var segments = uri.Segments;
        if (segments.Length == 0)
        {
            throw new InvalidOperationException("The URI does not contain any path segments.");
        }

        return segments.Last().TrimEnd('/');
    }

    public IAzureDevOpsServer AsInterface() => Mock.Object;


}
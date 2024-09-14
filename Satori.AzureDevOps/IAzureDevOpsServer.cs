using Satori.AzureDevOps.Models;

namespace Satori.AzureDevOps;

public interface IAzureDevOpsServer
{
    bool Enabled { get; }
    ConnectionSettings ConnectionSettings { get; }

    Task<PullRequest[]> GetPullRequestsAsync();
    Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequestId pr);
    Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds);
    Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds);

    /// <summary>
    /// Updates a work item
    /// </summary>
    /// <param name="id">Work Item ID</param>
    /// <param name="items">Items/fields on the work item to update</param>
    /// <returns></returns>
    Task<WorkItem> PatchWorkItemAsync(int id, IEnumerable<WorkItemPatchItem> items);

    Task<Team[]> GetTeamsAsync();
    Task<Iteration?> GetCurrentIterationAsync(Team team);
    Task<WorkItemRelation[]> GetIterationWorkItemsAsync(IterationId iteration);

    /// <summary>
    /// Reorder priority of work items
    /// </summary>
    /// <param name="iteration"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/workitemsorder/reorder-backlog-work-items?view=azure-devops-rest-6.0&tabs=HTTP
    /// </para>
    /// </remarks>
    ReorderResult[] ReorderBacklogWorkItems(IterationId iteration, ReorderOperation operation);

    Task<Guid> GetCurrentUserIdAsync();

    Task<Identity> GetIdentityAsync(Guid id);

}
using Satori.AzureDevOps.Models;

namespace Satori.AzureDevOps;

public interface IAzureDevOpsServer
{
    ConnectionSettings ConnectionSettings { get; }

    Task<PullRequest[]> GetPullRequestsAsync();
    Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequest pr);
    Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds);
    Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds);
    Task<Team[]> GetTeamsAsync();
    Task<Iteration?> GetCurrentIterationAsync(Team team);
    Task<WorkItemRelation[]> GetIterationWorkItemsAsync(IterationId iteration);

    /// <summary>
    /// Reorder priority of work items
    /// </summary>
    /// <param name="team"></param>
    /// <param name="operation"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/workitemsorder/reorder-backlog-work-items?view=azure-devops-rest-6.0&tabs=HTTP
    /// </para>
    /// </remarks>
    ReorderResult[] ReorderBacklogWorkItems(TeamId team, ReorderOperation operation);
}
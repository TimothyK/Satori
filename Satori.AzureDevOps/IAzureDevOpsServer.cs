﻿using Satori.AzureDevOps.Models;

namespace Satori.AzureDevOps;

public interface IAzureDevOpsServer
{
    bool Enabled { get; }
    ConnectionSettings ConnectionSettings { get; }

    Task<PullRequest[]> GetPullRequestsAsync();
    Task<PullRequest> GetPullRequestAsync(int pullRequestId);
    Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequestId pr);

    /// <summary>
    /// Gets the git tags (which are likely version numbers) of the commit where a pull request was merged.
    /// </summary>
    /// <param name="pullRequest"></param>
    /// <returns></returns>
    Task<Tag[]> GetTagsOfMergeAsync(PullRequest pullRequest);

    Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds);
    Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds);

    /// <summary>
    /// Updates a work item
    /// </summary>
    /// <param name="id">Work Item ID</param>
    /// <param name="items">Items/fields on the work item to update</param>
    /// <returns></returns>
    Task<WorkItem> PatchWorkItemAsync(int id, IEnumerable<WorkItemPatchItem> items);

    /// <summary>
    /// Creates a new Task
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    Task<WorkItem> PostWorkItemAsync(string projectName, IEnumerable<WorkItemPatchItem> items);

    /// <summary>
    /// Tests if a user has permission to create a work item
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    Task<bool> TestPostWorkItemAsync(string projectName, IEnumerable<WorkItemPatchItem> items);

    Task<Team[]> GetTeamsAsync();
    Task<Iteration?> GetCurrentIterationAsync(Team team);
    Task<WorkItemLink[]> GetIterationWorkItemsAsync(IterationId iteration);

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
    Task<ReorderResult[]> ReorderBacklogWorkItemsAsync(IterationId iteration, ReorderOperation operation);

    Task<Guid> GetCurrentUserIdAsync();

    Task<Identity> GetIdentityAsync(Guid id);

}
﻿using Satori.AzureDevOps.Models;

namespace Satori.AzureDevOps;

public interface IAzureDevOpsServer
{
    ConnectionSettings ConnectionSettings { get; }
    Task<PullRequest[]> GetPullRequestsAsync();
    Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequest pr);
    Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds);
    Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds);
}
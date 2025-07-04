﻿using Microsoft.Extensions.Logging;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.Services.Converters;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using PullRequestDto = Satori.AzureDevOps.Models.PullRequest;


namespace Satori.AppServices.Services;

public class PullRequestService(
    IAzureDevOpsServer azureDevOpsServer
    , ILoggerFactory loggerFactory
    , IAlertService alertService
)
{
    private IAzureDevOpsServer AzureDevOpsServer { get; } = azureDevOpsServer;

    private ILogger<PullRequestService> Logger => loggerFactory.CreateLogger<PullRequestService>();

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
    {
        var stopWatch = Stopwatch.StartNew();
        PullRequestDto[] pullRequests = [];
        try
        {
            pullRequests = await AzureDevOpsServer.GetPullRequestsAsync();
        }
        catch (Exception ex)
        {
            alertService.BroadcastAlert(ex);
            Logger.LogError(ex, "Failed to get pull requests");
        }
        Logger.LogDebug("Got {PullRequestCount} pull requests in {ElapsedMilliseconds}ms", pullRequests.Length, stopWatch.ElapsedMilliseconds);

        var viewModels = pullRequests.Select(PullRequestExtensions.ToViewModel).ToArray();

        return viewModels;
    }

    public async Task<PullRequest[]> AddWorkItemsToPullRequestsAsync(PullRequest[] pullRequests)
    {
        if (pullRequests.Length == 0)
        {
            return pullRequests;
        }

        var stopWatch = Stopwatch.StartNew();
        var workItemMap = await GetWorkItemMap(pullRequests.Select(x => (PullRequestId)x));
        Logger.LogDebug("WorkItem Map loaded in {ElapsedMilliseconds}ms", stopWatch.ElapsedMilliseconds);

        stopWatch = Stopwatch.StartNew();
        var workItemIds = workItemMap.SelectMany(kvp => kvp.Value).Distinct();
        var workItems = (await AzureDevOpsServer.GetWorkItemsAsync(workItemIds))
            .ToDictionary(wi => wi.Id, wi => wi.ToViewModel());
        Logger.LogDebug("Got {WorkItemCount} work items in {ElapsedMilliseconds}ms", workItems.Count, stopWatch.ElapsedMilliseconds);

        foreach (var pr in pullRequests)
        {
            if (workItemMap.TryGetValue(pr.Id, out var ids))
            {
                pr.WorkItems = ids.Select(workItemId => workItems[workItemId]).ToList();
            }
        }

        return pullRequests;
    }

    private async Task<Dictionary<int, List<int>>> GetWorkItemMap(IEnumerable<PullRequestId> pullRequestIds)
    {
        var pullRequestWorkItemMappings = new ConcurrentBag<(int pullRequestId, int workItemId)>();
        var options = new ParallelOptions() { MaxDegreeOfParallelism = 8 };
        await Parallel.ForEachAsync(pullRequestIds, options, async (prId, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var idMap = await AzureDevOpsServer.GetPullRequestWorkItemIdsAsync(prId);
            foreach (var workItemId in idMap.Select(map => map.Id))
            {
                pullRequestWorkItemMappings.Add((prId.Id, workItemId));
            }
        });

        return pullRequestWorkItemMappings
            .GroupBy(map => map.pullRequestId)
            .ToDictionary(g => g.Key, g => g.Select(map => map.workItemId).ToList());
    }
}
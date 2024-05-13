using Flurl;
using Microsoft.Extensions.Logging;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using PullRequestDto = Satori.AzureDevOps.Models.PullRequest;
using UriParser = Satori.AppServices.Services.Converters.UriParser;


namespace Satori.AppServices.Services;

public class PullRequestService(IAzureDevOpsServer azureDevOpsServer, ILoggerFactory loggerFactory)
{
    private IAzureDevOpsServer AzureDevOpsServer { get; } = azureDevOpsServer;

    private ILogger<PullRequestService> Logger => loggerFactory.CreateLogger<PullRequestService>();

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
    {
        var stopWatch = Stopwatch.StartNew();
        var pullRequests = await AzureDevOpsServer.GetPullRequestsAsync();
        Logger.LogDebug("Got {PullRequestCount} pull requests in {ElapsedMilliseconds}ms", pullRequests.Length, stopWatch.ElapsedMilliseconds);

        var viewModels = pullRequests.Select(ToViewModel).ToArray();

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


    private PullRequest ToViewModel(PullRequestDto pr)
    {
        var reviews = pr.Reviewers
            .Select(ToViewModel)
            .OrderByDescending(x => x.Vote)
            .ThenBy(x => x.Reviewer.DisplayName)
            .ToList();

        var projectName = pr.Repository.Project.Name;
        var repositoryName = pr.Repository.Name;
        var id = pr.PullRequestId;
        var pullRequest = new PullRequest
        {
            Id = id,
            Title = pr.Title,
            RepositoryName = repositoryName,
            Project = projectName,
            Status = pr.IsDraft ? Status.Draft : Status.Open,
            AutoComplete = !string.IsNullOrEmpty(pr.CompletionOptions?.MergeCommitMessage),
            CreationDate = pr.CreationDate,
            CreatedBy = pr.CreatedBy.ToViewModel(),
            Reviews = reviews,
            Labels = pr.Labels?.Where(label => label.Active).Select(label => label.Name).ToList() ?? [],
            WorkItems = [],
            Url = UriParser.GetAzureDevOpsOrgUrl(pr.Url)
                .AppendPathSegment(projectName)
                .AppendPathSegment("_git")
                .AppendPathSegment(repositoryName)
                .AppendPathSegment("pullRequest")
                .AppendPathSegment(id),
        };

        return pullRequest;
    }

    private static Review ToViewModel(Reviewer reviewer)
    {
        return new Review()
        {
            IsRequired = reviewer.IsRequired,
            Vote = (ReviewVote)reviewer.Vote,
            Reviewer = new Person()
            {
                Id = reviewer.Id,
                DisplayName = reviewer.DisplayName,
                AvatarUrl = reviewer.ImageUrl,
            },
        };
    }

}
using CodeMonkeyProjectiles.Linq;
using Flurl;
using MoreLinq;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.TimeServices;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AzureDevOps.Exceptions;
using Satori.Kimai;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using UriParser = Satori.AppServices.Services.Converters.UriParser;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

public class SprintBoardService(
    IAzureDevOpsServer azureDevOpsServer,
    ITimeServer timeServer,
    IAlertService alertService,
    ILoggerFactory loggerFactory,
    IKimaiServer kimai)
{
    private readonly IKimaiServer _kimai = kimai;

    #region GetActiveSptringsAsync

    public async Task<IEnumerable<Sprint>> GetActiveSprintsAsync()
    {
        Team[] teams = [];
        try
        {
            teams = await azureDevOpsServer.GetTeamsAsync();
        }
        catch (Exception ex)
        {
            alertService.BroadcastAlert(ex);
        }

        var iterations = await GetIterationsAsync(teams);

        return iterations.Select(map => ToViewModel(map.Team, map.Iteration));
    }

    private async Task<IEnumerable<(Team Team, Iteration Iteration)>> GetIterationsAsync(IEnumerable<Team> teams)
    {
        var iterations = new ConcurrentBag<(Team, Iteration)>();
        var options = new ParallelOptions() { MaxDegreeOfParallelism = 8 };
        await Parallel.ForEachAsync(teams, options, async (team, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var iteration = await azureDevOpsServer.GetCurrentIterationAsync(team);
            if (iteration?.Attributes.FinishDate == null || iteration.Attributes.FinishDate.Value.AddDays(7) <= timeServer.GetUtcNow())
            {
                return;
            }

            var hasPermission = await HasWritePermissionAsync(iteration, team);
            if (!hasPermission)
            {
                return;
            }

            iterations.Add((team, iteration));
        });

        return iterations;
    }

    private async Task<bool> HasWritePermissionAsync(Iteration iteration, Team team)
    {
        IEnumerable<WorkItemPatchItem> patches =
        [
            new() {Operation = Operation.Add, Path = "/fields/System.Title", Value = "TestTask"},
            new() {Operation = Operation.Add, Path = "/fields/System.IterationPath", Value = iteration.Path}
        ];
        var hasPermissions = await azureDevOpsServer.TestPostWorkItemAsync(team.ProjectName, patches);
        return hasPermissions;
    }

    private static Sprint ToViewModel(Team team, Iteration iteration)
    {
        var azureDevOpsUrl = UriParser.GetAzureDevOpsOrgUrl(team.Url);

        var projectName = team.ProjectName;
        var teamName = team.Name;
        var iterationPath = iteration.Path;
        var sprintBoardUrl = azureDevOpsUrl
            .AppendPathSegment(projectName)
            .AppendPathSegment("_sprints/taskBoard")
            .AppendPathSegment(teamName)
            .AppendPathSegment(iterationPath.Replace(@"\", "/"));

        var teamID = team.Id;
        var teamAvatarUrl = azureDevOpsUrl
            .AppendPathSegment("_api/_common/IdentityImage")
            .AppendQueryParam("id", teamID);

        return new Sprint()
        {
            Id = iteration.Id,
            Name = iteration.Name,
            IterationPath = iterationPath,
            StartTime = iteration.Attributes.StartDate ?? throw new InvalidOperationException($"Iteration {iterationPath} missing startDate"),
            FinishTime = iteration.Attributes.FinishDate ?? throw new InvalidOperationException($"Iteration {iterationPath} missing finishDate"),
            TeamId = teamID,
            TeamName = teamName,
            ProjectName = team.ProjectName,
            SprintBoardUrl = sprintBoardUrl,
            TeamAvatarUrl = teamAvatarUrl,
        };
    }

    #endregion GetActiveSptringsAsync

    #region GetWorkItemsAsync

    public async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(params Sprint[] sprints)
    {
        var workItems = new ConcurrentBag<WorkItem>();

        var options = new ParallelOptions() { MaxDegreeOfParallelism = 8 };

        await Parallel.ForEachAsync(sprints, options, async (sprint, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var iterationBoardItems = await GetWorkItemsAsync(sprint);

            foreach (var workItem in iterationBoardItems)
            {
                workItems.Add(workItem);
            }
        });

        return workItems;
    }

    private async Task<List<WorkItem>> GetWorkItemsAsync(Sprint sprint)
    {
        var iteration = (IterationId)sprint;

        var links = await azureDevOpsServer.GetIterationWorkItemsAsync(iteration);
        var workItemIds = links.Select(x => x.Target.Id).ToArray();
        var iterationWorkItems = (await azureDevOpsServer.GetWorkItemsAsync(workItemIds, _kimai)).ToList();

        foreach (var workItem in iterationWorkItems.OrderBy(wi => wi.AbsolutePriority))
        {
            workItem.Children.Clear();
            workItem.Sprint = sprint;
        }

        var iterationTasks = iterationWorkItems.Where(wi => wi.Type == WorkItemType.Task).ToDictionary(wi => wi.Id, wi => wi);
        var iterationBoardItems = iterationWorkItems
            .Where(wi => wi.Type.IsIn(WorkItemType.BoardTypes))
            .ToDictionary(wi => wi.Id, wi => wi);
        foreach (var link in links.Where(r => r.Source != null))
        {
            var parentWorkItemId = link.Source?.Id ?? throw new InvalidOperationException();
            var parent = iterationBoardItems[parentWorkItemId];

            if (iterationTasks.TryGetValue(link.Target.Id, out var task))
            {
                task.Parent = parent;
                parent.Children.Add(task);
            }
        }

        await ReplaceRelationPlaceholdersAsync(iterationWorkItems);

        iterationBoardItems.Values.ResetPeopleRelations();
        SetSprintPriority(iterationBoardItems.Values);

        return iterationBoardItems.Values.ToList();
    }

    private async Task ReplaceRelationPlaceholdersAsync(List<WorkItem> workItems)
    {
        var workItemsWithPlaceholders = workItems.Where(wi => wi.Predecessors.Any(linkedWorkItem => linkedWorkItem.Type == WorkItemType.Unknown))
            .Concat(workItems.Where(wi => wi.Successors.Any(linkedWorkItem => linkedWorkItem.Type == WorkItemType.Unknown)))
            .ToArray();

        var placeholderIds = 
            workItemsWithPlaceholders.SelectMany(workItem => workItem.Predecessors.Where(wi => wi.Type == WorkItemType.Unknown))
                .Concat(workItemsWithPlaceholders.SelectMany(workItem => workItem.Successors.Where(wi => wi.Type == WorkItemType.Unknown)))
                .Select(workItem => workItem.Id)
                .Distinct();

        var alreadyLoaded = workItems.Where(wi => wi.Id.IsIn(placeholderIds)).ToArray();

        var needToLoadIds = placeholderIds.Except(alreadyLoaded.Select(wi => wi.Id)).ToArray();

        var replacements = alreadyLoaded.Concat(await GetWorkItemsAsync(needToLoadIds)).ToArray();

        foreach (var workItem in workItemsWithPlaceholders)
        {
            ReplacePlaceholders(workItem.Predecessors, replacements);
            ReplacePlaceholders(workItem.Successors, replacements);
        }
    }

    private static void SetSprintPriority(IEnumerable<WorkItem> workItems)
    {
        foreach (var (sprintPriority, workItem) in workItems
                     .Where(wi => wi.State != ScrumState.Done)
                     .OrderBy(wi => wi.AbsolutePriority).ThenBy(wi => wi.Id)
                     .Select((wi, i) => (i, wi)))
        {
            workItem.SprintPriority = sprintPriority + 1;
        }
    }

    #endregion GetWorkItemsAsync

    #region RefreshWorkItemAsync

    /// <summary>
    /// Refreshes a work item for the Sprint Board
    /// </summary>
    /// <param name="allWorkItems">
    /// List of all work items.
    /// The <see cref="original"/> work item will be replaced in this List with the newly refreshed version (a different object).
    /// The relative sprint priorities will be recalculated.
    /// If the work item could not be refreshed or is no longer valid, it will be removed from this List.
    /// </param>
    /// <param name="original">Work Item to refresh</param>
    /// <remarks>
    /// <para>
    /// After the sprint board work items are loaded (via <see cref="GetWorkItemsAsync"/>) a single work item can be refreshed from this method.
    /// The original load method loads all work items, which can take a long time.
    /// The Sprint Board has links to allow users to open the Work Item in Azure DevOps in another browser tab.
    /// The user can make changes, then return the Satori Sprint Board.
    /// However, the changes won't be immediately visible on the Sprint Board.  It needs to be refreshed to reload the changes.
    /// Loading the entire Spring Board can take a long time.  This method allows the user to (quickly) refresh a single work item
    /// instead of the entire board.
    /// </para>
    /// </remarks>
    /// <returns></returns>
    public async Task RefreshWorkItemAsync(List<WorkItem> allWorkItems, WorkItem original)
    {
        var workItem = (await azureDevOpsServer.GetWorkItemsAsync(original.Id.Yield()))
            .SingleOrDefault();
        var target = workItem == null ? null 
            : await workItem.ToViewModelAsync(_kimai);

        if (target == null || target.State == ScrumState.Removed)
        {
            allWorkItems.Remove(original);
            return;
        }
        
        SafeSetSprint(target, original);
        target.Sprint ??= FindSprintOrDefault(allWorkItems, target);
        if (target.Sprint == null)
        {
            allWorkItems.Remove(original);
            return;
        }

        await GetChildWorkItemsAsync(target);

        await GetPullRequestsAsync(target.Yield().Concat(target.Children).ToArray());

        var index = allWorkItems.IndexOf(original);
        allWorkItems[index] = target;

        await ReplaceRelationPlaceholdersAsync(allWorkItems.Concat(allWorkItems.SelectMany(wi => wi.Children)).ToList());

        SetSprintPriority(allWorkItems.Where(wi => wi.Sprint == target.Sprint));
        allWorkItems.ResetPeopleRelations();
    }

    private static void SafeSetSprint(WorkItem target, WorkItem source)
    {
        if (!IsSameSprint(target, source))
        {
            return;
        }

        target.Sprint = source.Sprint;
    }

    private static Sprint? FindSprintOrDefault(IEnumerable<WorkItem> workItems, WorkItem target)
    {
        return workItems
            .FirstOrDefault(wi => IsSameSprint(target, wi))
            ?.Sprint;
    }

    private static bool IsSameSprint(WorkItem a, WorkItem b)
    {
        return a.ProjectName == b.ProjectName
               && a.IterationPath == b.IterationPath;
    }

    /// <summary>
    /// Loads the placeholder children work items
    /// </summary>
    /// <param name="workItem"></param>
    /// <returns></returns>
    private async Task GetChildWorkItemsAsync(WorkItem workItem)
    {
        var placeholderChildren = workItem.Children
            .Where(wi => wi.Type == WorkItemType.Unknown)
            .ToArray();
        if (placeholderChildren.None())
        {
            return;
        }

        var children = (await GetWorkItemsAsync(placeholderChildren.Select(wi => wi.Id))).ToArray();
        foreach (var child in children)
        {
            SafeSetSprint(child, workItem);
            if (child.Sprint == workItem.Sprint && child.State != ScrumState.Removed)
            {
                child.Parent = workItem;
            }
        }

        ReplacePlaceholders(workItem.Children, children.Where(child => child.Parent == workItem).ToArray());
    }

    private static void ReplacePlaceholders(List<WorkItem> list, WorkItem[] source)
    {
        var replacements = list
            .Where(wi => wi.Type == WorkItemType.Unknown)
            .Select(placeholder => source.FirstOrDefault(wi => wi.Id == placeholder.Id))
            .OfType<WorkItem>()
            .ToList();

        list.RemoveAll(wi => wi.Type == WorkItemType.Unknown);
        list.AddRange(replacements);
    }

    private async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(IEnumerable<int> workItemIds) =>
        await GetWorkItemsAsync(workItemIds.ToArray());

    private async Task<IEnumerable<WorkItem>> GetWorkItemsAsync(int[] workItemIds)
    {
        try
        {
            return await azureDevOpsServer.GetWorkItemsAsync(workItemIds, _kimai);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<StandUpService>();
            logger.LogError(ex, "Failed to load work items {WorkItemIds}", workItemIds);

            var badIds = workItemIds.Where(id => ex.Message.Contains($" {id} ")).ToList();
            if (badIds.Count > 0)
            {
                return await GetWorkItemsAsync(workItemIds.Except(badIds).ToArray());
            }

            return [];
        }
    }

    #endregion RefreshWorkItemAsync

    #region GetPullRequests

    public async Task GetPullRequestsAsync(WorkItem[] workItems)
    {
        var pullRequestIds = GetPullRequestIds(workItems);
        var pullRequests = await PullRequestsAsync(pullRequestIds);
        ReplacePullRequests(workItems, pullRequests);

        workItems.ResetPeopleRelations();
    }

    private static int[] GetPullRequestIds(WorkItem[] workItems)
    {
        var pullRequestIds = workItems.SelectMany(wi => wi.PullRequests)
            .Union(workItems.SelectMany(wi => wi.Children).SelectMany(task => task.PullRequests))
            .Select(pr => pr.Id)
            .Distinct()
            .ToArray();
        return pullRequestIds;
    }

    private async Task<Dictionary<int, PullRequest>> PullRequestsAsync(int[] pullRequestIds)
    {
        var pullRequests = new ConcurrentBag<PullRequest>();
        var options = new ParallelOptions() { MaxDegreeOfParallelism = 8 };

        await Parallel.ForEachAsync(pullRequestIds, options, async (prId, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            AzureDevOps.Models.PullRequest prDto;
            try
            {
                prDto = await azureDevOpsServer.GetPullRequestAsync(prId);
            }
            catch (Exception ex) when (ex.Message.Contains("TF401180"))
            {
                var logger = loggerFactory.CreateLogger<SprintBoardService>();
                logger.LogInformation(ex, "Failed to load PR {PullRequestId}", prId);
                return;
            }
            var prViewModel = prDto.ToViewModel();

            if (prViewModel.Status == Status.Complete)
            {
                try
                {
                    var tags = await azureDevOpsServer.GetTagsOfMergeAsync(prDto);
                    prViewModel.VersionTags = tags.Select(t => t.Name).ToArray();
                }
                catch (SecurityException)
                {
                    alertService.BroadcastAlert("Version tags of completed PRs is not available unless Full Control is granted to the Azure DevOps Personal Access Token");
                }
            }

            pullRequests.Add(prViewModel);
        });

        return pullRequests.ToDictionary(pr => pr.Id, pr => pr);
    }

    private static void ReplacePullRequests(WorkItem[] workItems, Dictionary<int, PullRequest> pullRequests)
    {
        foreach (var workItem in workItems.Concat(workItems.SelectMany(task => task.Children)))
        {
            var i = 0;
            while (i < workItem.PullRequests.Count)
            {
                var pr = pullRequests.GetValueOrDefault(workItem.PullRequests[i].Id);
                if (pr == null || pr.Status == Status.Abandoned)
                {
                    workItem.PullRequests.RemoveAt(i);
                }
                else
                {
                    workItem.PullRequests[i] = pr;
                    i++;
                }
            }
        }
    }

    #endregion GetPullRequests

    #region ReorderWorkItems

    public async Task ReorderWorkItemsAsync(ReorderRequest request)
    {
        if (request.WorkItemsToMove.Length == 0)
        {
            throw new InvalidOperationException("Work Items must be selected to be moved");
        }

        var orderByDirection = request.RelativeToTarget == RelativePosition.Below ? OrderByDirection.Ascending : OrderByDirection.Descending;
        var allWorkItems = request.AllWorkItems.OrderBy(wi => wi.AbsolutePriority, orderByDirection).ToArray();

        var nextWorkItem = request.Target ?? allWorkItems.Last();
        var previousWorkItem = allWorkItems.SkipUntil(wi => wi == request.Target).Take(1).FirstOrDefault();
        var operation = new ReorderOperation
        {
            PreviousId = nextWorkItem.Id,
            NextId = previousWorkItem?.Id ?? 0,
            Ids = request.WorkItemsToMove.Select(wi => wi.Id).ToArray()
        };
        if (request.RelativeToTarget == RelativePosition.Above)
        {
            (operation.PreviousId, operation.NextId) = (operation.NextId, operation.PreviousId);
        }

        var engine = new SprintTemporaryReassignmentEngine(azureDevOpsServer);
        engine.Add(request.WorkItemsToMove);
        engine.Add(previousWorkItem);
        await using (await engine.ReassignAsync(nextWorkItem))
        {
            var iteration = (IterationId)nextWorkItem.Sprint!;

            var reorderResults = await azureDevOpsServer.ReorderBacklogWorkItemsAsync(iteration, operation);

            foreach (var map in reorderResults.Join(request.WorkItemsToMove,
                         reorderResult => reorderResult.Id,
                         movingItem => movingItem.Id,
                         (reorderResult, movingItem) => new { WorkItem = movingItem, reorderResult.Order }))
            {
                map.WorkItem.AbsolutePriority = map.Order;
            }
        }

        var sprintGroups = request.AllWorkItems
            .GroupBy(wi => wi.Sprint!)
            .Where(g => g.Any(wi => wi.IsIn(request.WorkItemsToMove)));
        foreach (var sprintWorkItems in sprintGroups)
        {
            SetSprintPriority(sprintWorkItems);
        }
        request.AllWorkItems.ResetPeopleRelations();
    }

    #endregion ReorderWorkItems

}
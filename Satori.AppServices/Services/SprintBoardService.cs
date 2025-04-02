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
using Satori.AppServices.Services.Abstractions;
using UriParser = Satori.AppServices.Services.Converters.UriParser;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

public class SprintBoardService(
    IAzureDevOpsServer azureDevOpsServer
    , ITimeServer timeServer
    , IAlertService alertService
)
{
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

            iterations.Add((team, iteration));
        });

        return iterations;
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
        var workItemIds = links.Select(x => x.Target.Id);
        var items = await azureDevOpsServer.GetWorkItemsAsync(workItemIds);
        var iterationWorkItems = items.Select(wi => wi.ToViewModel()).ToList();
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
        SetSprintPriority(iterationBoardItems.Values);

        return iterationBoardItems.Values.ToList();
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

    #region ReorderWorkItems

    public async Task ReorderWorkItemsAsync(ReorderRequest request)
    {
        if (request.WorkItemsToMove.Length == 0)
        {
            throw new InvalidOperationException("Work Items must be selected to be moved");
        }

        var orderByDirection = request.RelativeToTarget == RelativePosition.Below ? OrderByDirection.Ascending : OrderByDirection.Descending;
        var allWorkItems = request.AllWorkItems.OrderBy(wi => wi.AbsolutePriority, orderByDirection).ToArray();

        var operation = new ReorderOperation
        {
            PreviousId = (request.Target ?? allWorkItems.Last()).Id,
            NextId = allWorkItems.SkipUntil(wi => wi == request.Target).Take(1).FirstOrDefault()?.Id ?? 0,
            Ids = request.WorkItemsToMove.Select(wi => wi.Id).ToArray()
        };

        if (request.RelativeToTarget == RelativePosition.Above)
        {
            (operation.PreviousId, operation.NextId) = (operation.NextId, operation.PreviousId);
        }

        var movingItems = request.WorkItemsToMove.OrderBy(wi => wi.AbsolutePriority).ToArray();

        do
        {
            var sprint = movingItems.First().Sprint!;
            var iteration = (IterationId)sprint;

            var items = movingItems.TakeWhile(wi => wi.Sprint == sprint).ToArray();
            operation.Ids = items.Select(wi => wi.Id).ToArray();

            var reorderResults = await azureDevOpsServer.ReorderBacklogWorkItemsAsync(iteration, operation);

            foreach (var map in reorderResults.Join(movingItems,
                         reorderResult => reorderResult.Id,
                         movingItem => movingItem.Id,
                         (reorderResult, movingItem) => new { WorkItem = movingItem, reorderResult.Order }))
            {
                map.WorkItem.AbsolutePriority = map.Order;
            }

            operation.PreviousId = items.Last().Id;
            movingItems = movingItems.Except(items).OrderBy(wi => wi.AbsolutePriority).ToArray();
        } while (movingItems.Length > 0);

        var sprintGroups = request.AllWorkItems
            .GroupBy(wi => wi.Sprint!)
            .Where(g => g.Any(wi => wi.IsIn(request.WorkItemsToMove)));
        foreach (var sprintWorkItems in sprintGroups)
        {
            SetSprintPriority(sprintWorkItems);
        }
    }

    #endregion ReorderWorkItems

}
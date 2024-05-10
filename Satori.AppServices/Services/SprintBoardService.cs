using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.TimeServices;
using System.Collections.Concurrent;
using UriParser = Satori.AppServices.Services.Converters.UriParser;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

public class SprintBoardService(IAzureDevOpsServer azureDevOpsServer, ITimeServer timeServer)
{
    public async Task<IEnumerable<Sprint>> GetActiveSprintsAsync()
    {
        var teams = await azureDevOpsServer.GetTeamsAsync();

        var iterations = await GetIterationsAsync(teams);

        return iterations.Select(map => ToViewModel(map.Team, map.Iteration));
    }

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
        var iteration = new IterationId()
        {
            Id = sprint.Id,
            IterationPath = sprint.IterationPath,
            TeamName = sprint.TeamName,
            ProjectName = sprint.ProjectName
        };

        var relations = await azureDevOpsServer.GetIterationWorkItemsAsync(iteration);
        var workItemIds = relations.Select(x => x.Target.Id);
        var items = await azureDevOpsServer.GetWorkItemsAsync(workItemIds);
        var iterationWorkItems = items.Select(wi => wi.ToViewModel()).ToList();
        foreach (var workItem in iterationWorkItems.OrderBy(wi => wi.AbsolutePriority))
        {
            workItem.Sprint = sprint;
        }

        var iterationTasks = iterationWorkItems.Where(wi => wi.Type == WorkItemType.Task).ToDictionary(wi => wi.Id, wi => wi);
        var iterationBoardItems = iterationWorkItems
            .Where(wi => wi.Type.IsIn(WorkItemType.BoardTypes))
            .ToDictionary(wi => wi.Id, wi => wi);
        foreach (var relation in relations.Where(r => r.Source != null))
        {
            var parentWorkItemId = relation.Source?.Id ?? throw new InvalidOperationException();
            var parent = iterationBoardItems[parentWorkItemId];

            if (iterationTasks.TryGetValue(relation.Target.Id, out var task))
            {
                task.Parent = parent;
                parent.Children.Add(task);
            }
        }
        foreach (var (sprintPriority, workItem) in iterationBoardItems.Values
                     .Where(wi => wi.State != ScrumState.Done)
                     .OrderBy(wi => wi.AbsolutePriority).ThenBy(wi => wi.Id)
                     .Select((wi, i) => (i, wi)))
        {
            workItem.SprintPriority = sprintPriority + 1;
        }

        return iterationBoardItems.Values.ToList();
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
}
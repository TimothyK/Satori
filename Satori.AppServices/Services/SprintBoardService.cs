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
        var workItems = new List<WorkItem>();

        foreach (var sprint in sprints)
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
            
            var iterationTasks = iterationWorkItems.Where(wi => wi.Type == WorkItemType.Task).ToArray();
            var iterationBoardItems = iterationWorkItems.Where(wi => wi.Type == WorkItemType.ProductBacklogItem || wi.Type == WorkItemType.Bug).ToList();
            foreach (var relation in relations)
            {
                var parentWorkItemId = relation.Source?.Id;
                if (parentWorkItemId != null)
                {
                    var parent = iterationBoardItems.FirstOrDefault(wi => wi.Id == parentWorkItemId);
                    var task = iterationTasks.FirstOrDefault(wi => wi.Id == relation.Target.Id);

                    if (parent != null && task != null)
                    {
                        task.Parent = parent;
                        parent.Children.Add(task);
                    }
                }
            }

            workItems.AddRange(iterationBoardItems);
        }

        return workItems;
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
            if (iteration?.Attributes.FinishDate == null || iteration.Attributes.FinishDate.Value.AddDays(1) <= timeServer.GetUtcNow())
            {
                return;
            }

            iterations.Add((team, iteration));
        });

        return iterations;
    }
}
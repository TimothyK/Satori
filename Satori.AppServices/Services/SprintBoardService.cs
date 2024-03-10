using Flurl;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.TimeServices;
using System.Collections.Concurrent;

namespace Satori.AppServices.Services;

public class SprintBoardService(IAzureDevOpsServer azureDevOpsServer, ITimeServer timeServer)
{
    public async Task<IEnumerable<Sprint>> GetActiveSprintsAsync()
    {
        var teams = await azureDevOpsServer.GetTeamsAsync();

        var iterations = await GetIterationsAsync(teams);

        return iterations.Select(map => ToViewModel(map.Team, map.Iteration));
    }

    private Sprint ToViewModel(Team team, Iteration iteration)
    {
        var projectName = team.ProjectName;
        var teamName = team.Name;
        var iterationPath = iteration.Path;
        var sprintBoardUrl = azureDevOpsServer.ConnectionSettings.Url
            .AppendPathSegment(projectName)
            .AppendPathSegment("_sprints/taskBoard")
            .AppendPathSegment(teamName)
            .AppendPathSegment(iterationPath.Replace(@"\", "/"));

        var teamID = team.Id;
        var teamAvatarUrl = azureDevOpsServer.ConnectionSettings.Url
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
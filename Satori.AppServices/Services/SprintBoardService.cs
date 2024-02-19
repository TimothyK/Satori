using Flurl;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using System.Collections.Concurrent;

namespace Satori.AppServices.Services
{
    public class SprintBoardService(IAzureDevOpsServer azureDevOpsServer)
    {
        public async Task<IEnumerable<Sprint>> GetActiveSprintsAsync()
        {
            var teams = await azureDevOpsServer.GetTeamsAsync();

            var iterations = await GetIterationsAsync(teams);

            return iterations.Select(map => ToViewModel(map.Team, map.Iteration));
        }

        private Sprint ToViewModel(Team team, Iteration iteration)
        {
            var projectName = team.projectName;
            var teamName = team.name;
            var iterationPath = iteration.path;
            var sprintBoardUrl = azureDevOpsServer.ConnectionSettings.Url
                .AppendPathSegment(projectName)
                .AppendPathSegment("_sprints/taskBoard")
                .AppendPathSegment(teamName)
                .AppendPathSegment(iterationPath.Replace(@"\", "/"));

            var teamID = team.id;
            var teamAvatarUrl = azureDevOpsServer.ConnectionSettings.Url
                .AppendPathSegment("_api/_common/IdentityImage")
                .AppendQueryParam("id", teamID);

            return new Sprint()
            {
                Id = iteration.id,
                Name = iteration.name,
                IterationPath = iterationPath,
                StartTime = iteration.attributes.startDate ?? throw new InvalidOperationException("Iteration missing startDate"),
                FinishTime = iteration.attributes.finishDate ?? throw new InvalidOperationException("Iteration missing finishDate"),
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
                if (iteration != null)
                {
                    iterations.Add((team, iteration));
                }

            });

            return iterations;
        }
    }
}

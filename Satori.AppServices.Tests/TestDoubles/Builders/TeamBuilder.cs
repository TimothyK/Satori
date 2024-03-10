using Satori.AppServices.Tests.TestDoubles.Database;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Builders
{
    internal class TeamBuilder
    {
        private readonly IAzureDevOpsDatabaseWriter _database;

        public TeamBuilder(IAzureDevOpsDatabaseWriter database)
        {
            _database = database;
            Team = BuildTeam();
            _database.AddTeam(Team);
        }

        public Team Team { get; }

        private static Team BuildTeam()
        {
            var team = Builder.Builder<Team>.New().Build(int.MaxValue);
            return team;
        }

        public TeamBuilder WithIteration()
        {
            return WithIteration(out _);
        }

        public TeamBuilder WithIteration(out Iteration iteration)
        {
            iteration = BuildIteration();
            return WithIteration(iteration);
        }
        
        public TeamBuilder WithIteration(Iteration iteration)
        {
            _database.LinkIteration(Team, iteration);
            return this;
        }

        private static Iteration BuildIteration()
        {
            var iteration = Builder.Builder<Iteration>.New().Build(int.MaxValue);
            iteration.Attributes.StartDate = DateTime.UtcNow.AddDays(-2);
            iteration.Attributes.FinishDate = DateTime.UtcNow.AddDays(2);
            iteration.Attributes.TimeFrame = "current";
            return iteration;
        }

    }
}

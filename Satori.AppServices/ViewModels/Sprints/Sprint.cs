namespace Satori.AppServices.ViewModels.Sprints
{
    public class Sprint
    {
        public Guid Id { get; init; }
        public required string Name { get; init; }  
        public required string IterationPath { get; init; }  

        public DateTimeOffset StartTime { get; init; }
        public DateTimeOffset FinishTime { get; init; }

        public required Guid TeamId { get; init; }
        public required string TeamName { get; init; }

        public required string TeamAvatarUrl { get; init; }
        public required string SprintBoardUrl { get; init; }
    }
}

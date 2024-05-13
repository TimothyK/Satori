using Satori.AzureDevOps.Models;

namespace Satori.AppServices.ViewModels.Sprints;

public class Sprint
{
    /// <summary>
    /// IterationId
    /// </summary>
    public Guid Id { get; init; }
    public required string Name { get; init; }  
    public required string IterationPath { get; init; }  

    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset FinishTime { get; init; }

    public required Guid TeamId { get; init; }
    public required string TeamName { get; init; }

    public required string TeamAvatarUrl { get; init; }
    public required string SprintBoardUrl { get; init; }
    public required string ProjectName { get; init; }

    public static implicit operator IterationId(Sprint sprint) =>
        new()
        {
            Id = sprint.Id , 
            IterationPath = sprint.IterationPath, 
            TeamId = sprint.TeamId,
            TeamName = sprint.TeamName,
            ProjectName = sprint.ProjectName,
        };
}
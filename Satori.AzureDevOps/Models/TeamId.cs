namespace Satori.AzureDevOps.Models;

public class TeamId
{
    /// <summary>
    /// ID of the Team
    /// </summary>
    public Guid Id { get; init; }
    public string? TeamName { get; init; }
    public required string ProjectName { get; init; }
}
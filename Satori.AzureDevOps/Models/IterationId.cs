namespace Satori.AzureDevOps.Models;

public class IterationId
{
    /// <summary>
    /// ID of the Iteration
    /// </summary>
    public Guid Id { get; init; }
    public required string IterationPath { get; init; }
    public required string TeamName { get; init; }
    public required string ProjectName { get; init; }
}
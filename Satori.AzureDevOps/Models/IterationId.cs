namespace Satori.AzureDevOps.Models;

public class IterationId
{
    /// <summary>
    /// ID of the Iteration
    /// </summary>
    public Guid Id { get; set; }
    public required string TeamName { get; set; }
    public required string ProjectName { get; set; }
}
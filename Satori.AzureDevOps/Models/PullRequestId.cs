namespace Satori.AzureDevOps.Models;

public class PullRequestId
{
    /// <summary>
    /// PullRequestId
    /// </summary>
    public int Id { get; init; }
    public required string RepositoryName { get; init; }
    public required string ProjectName { get; init; }
}
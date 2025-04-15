using Satori.AzureDevOps.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.ViewModels.PullRequests;

public class PullRequest
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public required string RepositoryName { get; init; }
    public required string Project { get; init; }
    public required string Url { get; init; }
    public required Status Status { get; init; }
    public bool AutoComplete { get; init; }  //set if /completionOptions/mergeCommitMessage has a value
    public DateTimeOffset CreationDate { get; init; }
    public required Person CreatedBy { get; init; }
    public required List<Review> Reviews { get; init; }
    public required List<WorkItem> WorkItems { get; set; }

    /// <summary>
    /// PR Tags
    /// </summary>
    public required List<string> Labels { get; init; }

    //TODO: Add Comments

    public static implicit operator PullRequestId(PullRequest pr) =>
        new()
        {
            Id = pr.Id,
            RepositoryName = pr.RepositoryName,
            ProjectName = pr.Project,
        };
}
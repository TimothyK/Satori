using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.ViewModels.PullRequests;

public class PullRequest
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public required string RepositoryName { get; init; }
    public required string Project { get; init; }
    public string Url { get; set; }
    public Status Status { get; init; }
    public bool AutoComplete { get; set; }  //set if /completionOptions/mergeCommitMessage has a value
    public DateTimeOffset CreationDate { get; init; }
    public required Person CreatedBy { get; init; }
    public required List<Review> Reviews { get; init; }
    public List<WorkItem> WorkItems { get; set; }

    /// <summary>
    /// PR Tags
    /// </summary>
    public required List<string> Labels { get; init; }

    //TODO: Add Comments
}
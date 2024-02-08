namespace Satori.AzureDevOps.Models;

public class PullRequest
{
    public int codeReviewId { get; set; }
    public User createdBy { get; set; }
    public DateTimeOffset creationDate { get; set; }
    public string description { get; set; }
    public bool isDraft { get; set; }
    public Commit lastMergeCommit { get; set; }
    public Commit lastMergeSourceCommit { get; set; }
    public Commit lastMergeTargetCommit { get; set; }
    public Guid mergeId { get; set; }
    public string mergeStatus { get; set; }
    public int pullRequestId { get; set; }
    public Repository repository { get; set; }
    public Reviewer[] reviewers { get; set; }
    public string sourceRefName { get; set; }
    public string status { get; set; }
    public bool supportsIterations { get; set; }
    public string targetRefName { get; set; }
    public string title { get; set; }
    public string url { get; set; }
    public CompletionOptions completionOptions { get; set; }
    public User autoCompleteSetBy { get; set; }
    public Label[] labels { get; set; }
}
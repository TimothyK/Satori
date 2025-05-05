using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.PullRequests.ActionItems;

public abstract class PullRequestActionItem(PullRequest pullRequest, string message, params Person[] people)
    : ActionItem(message, people)
{
    public PullRequest PullRequest { get; set; } = pullRequest;
}

public class PublishActionItem(PullRequest pr) 
    : PullRequestActionItem(pr, "The draft PR needs published", pr.CreatedBy);

public class CompleteActionItem(PullRequest pr) 
    : PullRequestActionItem(pr, "Complete the PR or add a reviewer", pr.CreatedBy);

public class ReviewActionItem(PullRequest pr, Person reviewer)
    : PullRequestActionItem(pr, "The PR is ready for review", reviewer);

public class ReplyActionItem(PullRequest pr)
    : PullRequestActionItem(pr, "A reply is needed for the reviewer's comment(s)", pr.CreatedBy);
using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.AppServices.ViewModels.PullRequests.ActionItems;

public abstract class PullRequestActionItem(PullRequest pullRequest, string actionDescription, params Person[] people)
    : ActionItem(actionDescription, people)
{
    public PullRequest PullRequest { get; set; } = pullRequest;
}

public class PublishActionItem(PullRequest pr) 
    : PullRequestActionItem(pr, "Publish", pr.CreatedBy);

public class CompleteActionItem(PullRequest pr) 
    : PullRequestActionItem(pr, "Complete", pr.CreatedBy);

public class ReviewActionItem(PullRequest pr, Person reviewer)
    : PullRequestActionItem(pr, "Review", reviewer);

public class ReplyActionItem(PullRequest pr)
    : PullRequestActionItem(pr, "Reply", pr.CreatedBy);
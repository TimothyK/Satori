namespace Satori.ViewModels.PullRequests;

public class Review
{
    public required Person Reviewer { get; set; }
    public bool IsRequired { get; set; }
    public ReviewVote Vote { get; set; }
}
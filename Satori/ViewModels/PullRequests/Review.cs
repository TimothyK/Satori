namespace Satori.ViewModels.PullRequests;

public class Review
{
    public Guid Id { get; set; }
    public Person Reviewer { get; set; }
    public bool IsRequired { get; set; }
    public ReviewVote Vote { get; set; }
}
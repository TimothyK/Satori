namespace Satori.AppServices.ViewModels.PullRequests;

public enum ReviewVote
{
    Approved = 10,
    ApprovedWithSuggestions = 5,
    NoVote = 0,
    WaitingForAuthor = -5,
    Rejected = -10,
}
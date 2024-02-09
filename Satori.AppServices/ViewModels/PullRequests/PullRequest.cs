namespace Satori.AppServices.ViewModels.PullRequests;

public class PullRequest
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string RepositoryName { get; set; }
    public string Project { get; set; }  // Project /repository/project/name  {CD, CQ, Shared}
    public string Url { get; set; }
    public Status Status { get; set; }
    public bool AutoComplete { get; set; }  //set if /completionOptions/mergeCommitMessage has a value
    public DateTimeOffset CreationDate { get; set; }
    public Person CreatedBy { get; set; }
    public List<Review> Reviews { get; set; }
    //TODO: Add WorkItems and Comments
}
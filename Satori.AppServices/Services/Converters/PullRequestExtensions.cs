using Flurl;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AzureDevOps.Models;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using PullRequestDto = Satori.AzureDevOps.Models.PullRequest;

namespace Satori.AppServices.Services.Converters;

internal static class PullRequestExtensions
{
    public static PullRequest ToViewModel(this PullRequestDto pr)
    {
        ArgumentNullException.ThrowIfNull(pr);

        var reviews = pr.Reviewers
            .Select(ToViewModel)
            .OrderByDescending(x => x.Vote)
            .ThenBy(x => x.Reviewer.DisplayName)
            .ToList();

        var projectName = pr.Repository.Project.Name;
        var repositoryName = pr.Repository.Name;
        var id = pr.PullRequestId;
        var pullRequest = new PullRequest
        {
            Id = id,
            Title = pr.Title,
            RepositoryName = repositoryName,
            Project = projectName,
            Status = pr.IsDraft ? Status.Draft : Status.FromApiValue(pr.Status),
            AutoComplete = !string.IsNullOrEmpty(pr.CompletionOptions?.MergeCommitMessage),
            CreationDate = pr.CreationDate,
            CreatedBy = pr.CreatedBy,
            Reviews = reviews,
            Labels = pr.Labels?.Where(label => label.Active).Select(label => label.Name).ToList() ?? [],
            WorkItems = [],
            Url = UriParser.GetAzureDevOpsOrgUrl(pr.Url)
                .AppendPathSegment(projectName)
                .AppendPathSegment("_git")
                .AppendPathSegment(repositoryName)
                .AppendPathSegment("pullRequest")
                .AppendPathSegment(id),
        };

        return pullRequest;
    }

    private static Review ToViewModel(Reviewer reviewer)
    {
        return new Review()
        {
            IsRequired = reviewer.IsRequired,
            Vote = (ReviewVote)reviewer.Vote,
            Reviewer = reviewer,
        };
    }
}
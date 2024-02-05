using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.ViewModels;
using Satori.ViewModels.PullRequests;

namespace Satori.Services
{
    public class PullRequestService
    {
        public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
        {
            
            var srv = new AzureDevOpsServer(Program.AzureDevOpsConnectionSettings);
            var pullRequests = await srv.GetPullRequestsAsync();
            return pullRequests.Select(ToViewModel).ToArray();
        }

        private static PullRequest ToViewModel(Value pr)
        {
            var reviews = pr.reviewers
                .Select(ToViewModel)
                .OrderByDescending(x => x.Vote)
                .ThenBy(x => x.Reviewer.DisplayName)
                .ToList();

            var pullRequest = new PullRequest
            {
                Id = pr.pullRequestId,
                Title = pr.title,
                RepositoryName = pr.repository.name,
                Project = pr.repository.project.name,
                Status = pr.isDraft ? Status.Draft : Status.Open,
                AutoComplete = !string.IsNullOrEmpty(pr.completionOptions?.mergeCommitMessage),
                CreationDate = pr.creationDate,
                CreatedBy = ToViewModel(pr.createdBy),
                Reviews = reviews,
            };

            pullRequest.Url = string.Format("https://devops.mayfield.pscl.com/PSDev/{0}/_git/{1}/pullrequest/{2}",
                pullRequest.Project, pullRequest.RepositoryName, pullRequest.Id);

            return pullRequest;
        }

        private static Review ToViewModel(Reviewer reviewer)
        {
            return new Review()
            {
                Id = new Guid(reviewer.id),
                IsRequired = reviewer.isRequired,
                Vote = (ReviewVote)reviewer.vote,
                Reviewer = new Person()
                {
                    Id = reviewer.uniqueName,
                    DisplayName = reviewer.displayName,
                    AvatarUrl = reviewer.imageUrl,
                },
            };
        }

        private static Person ToViewModel(Createdby user)
        {
            return new Person()
            {
                Id = user.uniqueName,
                DisplayName = user.displayName,
                AvatarUrl = user.imageUrl,
            };
        }

    }
}

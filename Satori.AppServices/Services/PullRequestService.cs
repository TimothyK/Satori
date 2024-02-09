using Flurl;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using ConnectionSettings = Satori.AppServices.Models.ConnectionSettings;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using PullRequestDto = Satori.AzureDevOps.Models.PullRequest;

namespace Satori.AppServices.Services
{
    public class PullRequestService
    {
        private readonly ConnectionSettings _connectionSettings;

        public PullRequestService(ConnectionSettings connectionSettings)
        {
            _connectionSettings = connectionSettings;
        }
        public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
        {
            var srv = new AzureDevOpsServer(_connectionSettings.AzureDevOps);
            var pullRequests = await srv.GetPullRequestsAsync();

            var workItemMap = new Dictionary<int, List<int>>();
            foreach (var pr in pullRequests)
            {
                var idMap = await srv.GetPullRequestWorkItemIdsAsync(pr);
                var workItemIds = idMap.Select(x => int.Parse(x.id)).ToList();
                workItemMap.Add(pr.pullRequestId, workItemIds);
            }

            var viewModels = pullRequests.Select(ToViewModel).ToArray();
            foreach (var pr in viewModels)
            {
                pr.WorkItems = workItemMap[pr.Id].Select(workItemId => new WorkItem() {Id = workItemId}).ToList();
            }

            return viewModels;
        }

        private PullRequest ToViewModel(PullRequestDto pr)
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

            pullRequest.Url = _connectionSettings.AzureDevOps.Url
                .AppendPathSegment(pullRequest.Project)
                .AppendPathSegment("_git")
                .AppendPathSegment(pullRequest.RepositoryName)
                .AppendPathSegment("pullRequest")
                .AppendPathSegment(pullRequest.Id);

            return pullRequest;
        }

        private static Review ToViewModel(Reviewer reviewer)
        {
            return new Review()
            {
                IsRequired = reviewer.isRequired,
                Vote = (ReviewVote)reviewer.vote,
                Reviewer = new Person()
                {
                    Id = reviewer.id,
                    DisplayName = reviewer.displayName,
                    AvatarUrl = reviewer.imageUrl,
                },
            };
        }

        private static Person ToViewModel(User user)
        {
            return new Person()
            {
                Id = user.id,
                DisplayName = user.displayName,
                AvatarUrl = user.imageUrl,
            };
        }

    }
}

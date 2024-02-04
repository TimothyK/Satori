using Satori.Services.Models;
using Satori.ViewModels;
using Satori.ViewModels.PullRequests;
using System.Text.Json;

namespace Satori.Services
{
    public class AzureDevOpsServer
    {
        public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://devops.mayfield.pscl.com/PSDev/_apis/git/pullrequests/?api-version=6.0");

            var token = Program.AzureDevOpsToken;
            request.Headers.Add("Authorization", $"Basic {token}");

            using var client = new HttpClient();
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Azure DevOps returned {(int)response.StatusCode} {response.StatusCode}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var root = await JsonSerializer.DeserializeAsync<Rootobject>(responseStream);

            return root.value.Select(ToViewModel).ToArray();
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

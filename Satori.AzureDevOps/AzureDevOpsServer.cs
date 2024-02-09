using Flurl;
using Pscl.CommaSeparatedValues;
using Satori.AzureDevOps.Models;
using System.Text.Json;

namespace Satori.AzureDevOps
{
    public class AzureDevOpsServer
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly HttpClient _httpClient;

        public AzureDevOpsServer(ConnectionSettings connectionSettings) : this(connectionSettings, new HttpClient())
        {
        }
        public AzureDevOpsServer(ConnectionSettings connectionSettings, HttpClient httpClient)
        {
            _connectionSettings = connectionSettings;
            _httpClient = httpClient;
        }

        public async Task<PullRequest[]> GetPullRequestsAsync()
        {
            var url = _connectionSettings.Url
                .AppendPathSegment("_apis/git/pullRequests")
                .AppendQueryParam("api-version", "6.0");

            return await GetAsync<PullRequest>(url);
        }

        public async Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequest pr)
        {
            var url = _connectionSettings.Url
                .AppendPathSegment(pr.repository.project.name)
                .AppendPathSegment("_apis/git/repositories")
                .AppendPathSegment(pr.repository.name)
                .AppendPathSegment("pullRequests")
                .AppendPathSegment(pr.pullRequestId)
                .AppendPathSegment("workItems")
                .AppendQueryParam("api-version", "6.0");

            return await GetAsync<IdMap>(url);
        }

        public async Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds) => await GetWorkItemsAsync(workItemIds.ToArray());
        public async Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds)
        {
            var url = _connectionSettings.Url
                .AppendPathSegment("_apis/wit/workItems")
                .AppendQueryParam("ids", workItemIds.ToCommaSeparatedValues())
                .AppendQueryParam("api-version", "6.0");

            return await GetAsync<WorkItem>(url);
        }

        private async Task<T[]> GetAsync<T>(Url url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("Authorization", $"Basic {_connectionSettings.PersonalAccessToken}");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Azure DevOps returned {(int)response.StatusCode} {response.StatusCode}");
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var root = await JsonSerializer.DeserializeAsync<RootObject<T>>(responseStream)
                       ?? throw new ApplicationException("Server did not respond");

            return root.value;
        }
    }
}

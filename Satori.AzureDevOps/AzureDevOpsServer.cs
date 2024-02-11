using Flurl;
using Pscl.CommaSeparatedValues;
using Satori.AzureDevOps.Models;
using System.Text.Json;

namespace Satori.AzureDevOps;

public class AzureDevOpsServer : IAzureDevOpsServer
{
    public ConnectionSettings ConnectionSettings { get; }
    private readonly HttpClient _httpClient;

    public AzureDevOpsServer(ConnectionSettings connectionSettings, HttpClient httpClient)
    {
        ConnectionSettings = connectionSettings;
        _httpClient = httpClient;
    }

    public async Task<PullRequest[]> GetPullRequestsAsync()
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
            .AppendQueryParam("api-version", "6.0");

        return await GetAsync<PullRequest>(url);
    }

    public async Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequest pr)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment(pr.Repository.Project.Name)
            .AppendPathSegment("_apis/git/repositories")
            .AppendPathSegment(pr.Repository.Name)
            .AppendPathSegment("pullRequests")
            .AppendPathSegment(pr.PullRequestId)
            .AppendPathSegment("workItems")
            .AppendQueryParam("api-version", "6.0");

        return await GetAsync<IdMap>(url);
    }

    public async Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds) => await GetWorkItemsAsync(workItemIds.ToArray());
    public async Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendQueryParam("ids", workItemIds.ToCommaSeparatedValues())
            .AppendQueryParam("api-version", "6.0");

        return await GetAsync<WorkItem>(url);
    }

    private async Task<T[]> GetAsync<T>(Url url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Add("Authorization", $"Basic {ConnectionSettings.PersonalAccessToken}");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Azure DevOps returned {(int)response.StatusCode} {response.StatusCode}");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var root = await JsonSerializer.DeserializeAsync<RootObject<T>>(responseStream)
                   ?? throw new ApplicationException("Server did not respond");

        return root.Value;
    }
}
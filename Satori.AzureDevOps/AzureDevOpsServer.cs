using Flurl;
using Microsoft.Extensions.Logging;
using Pscl.CommaSeparatedValues;
using Satori.AzureDevOps.Exceptions;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Services;
using System.Text.Json;

namespace Satori.AzureDevOps;

public class AzureDevOpsServer(
    ConnectionSettings connectionSettings
    , HttpClient httpClient
    , ITimeServer timeServer
    , ILoggerFactory loggerFactory
) : IAzureDevOpsServer
{
    public ConnectionSettings ConnectionSettings { get; } = connectionSettings;

    private ILogger<AzureDevOpsServer> Logger => loggerFactory.CreateLogger<AzureDevOpsServer>();

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

    public async Task<Team[]> GetTeamsAsync()
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/teams")
            .AppendQueryParam("api-version", "6.0-preview.2");

        return await GetAsync<Team>(url);
    }

    public async Task<Iteration?> GetCurrentIterationAsync(Team team)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegments(team.projectName, team.name)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendQueryParam("$timeframe", "Current")
            .AppendQueryParam("api-version", "6.1-preview");

        Iteration? iteration;
        try
        {
            iteration = (await GetAsync<Iteration>(url)).SingleOrDefault();
        }
        catch (AzureHttpRequestException ex) when (ex.TypeKey == "CurrentIterationDoesNotExistException")
        {
            return null;
        }

        if (iteration?.attributes.finishDate == null || iteration.attributes.finishDate < timeServer.GetUtcNow())
        {
            return null;
        }
        return iteration;
    }

    private async Task<T[]> GetAsync<T>(Url url)
    {
        Logger.LogInformation("GET {Url}", url);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeader(request);

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var root = await JsonSerializer.DeserializeAsync<RootObject<T>>(responseStream)
                   ?? throw new ApplicationException("Server did not respond");

        return root.Value;
    }

    private static async Task VerifySuccessfulResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw await AzureHttpRequestException.FromResponseAsync(response);
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        var userNamePasswordPair = $":{ConnectionSettings.PersonalAccessToken}";
        var cred = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(userNamePasswordPair));
        request.Headers.Add("Authorization", $"Basic {cred}");
    }
}
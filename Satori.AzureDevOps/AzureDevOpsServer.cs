using Flurl;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Satori.AzureDevOps.Exceptions;
using Satori.AzureDevOps.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Satori.AzureDevOps;

public class AzureDevOpsServer(
    ConnectionSettings connectionSettings
    , HttpClient httpClient
    , ILoggerFactory loggerFactory
) : IAzureDevOpsServer
{
    public bool Enabled => ConnectionSettings.Enabled;

    public ConnectionSettings ConnectionSettings { get; } = connectionSettings;

    private ILogger<AzureDevOpsServer> Logger => loggerFactory.CreateLogger<AzureDevOpsServer>();

    public async Task<PullRequest[]> GetPullRequestsAsync()
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
            .AppendQueryParam("api-version", "6.0");

        return await GetRootValueAsync<PullRequest>(url);
    }

    public async Task<IdMap[]> GetPullRequestWorkItemIdsAsync(PullRequestId pr)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment(pr.ProjectName)
            .AppendPathSegment("_apis/git/repositories")
            .AppendPathSegment(pr.RepositoryName)
            .AppendPathSegment("pullRequests")
            .AppendPathSegment(pr.Id)
            .AppendPathSegment("workItems")
            .AppendQueryParam("api-version", "6.0");

        return await GetRootValueAsync<IdMap>(url);
    }

    public async Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds) => await GetWorkItemsAsync(workItemIds.ToArray());
    /// <summary>
    /// Gets work items by their ids.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/list?view=azure-devops-rest-6.0
    /// </para>
    /// </remarks>
    /// <param name="workItemIds"></param>
    /// <returns></returns>
    public async Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds)
    {
        const int bucketSize = 200;
        if (workItemIds.Length == 0)
        {
            return [];
        }
        if (workItemIds.Length > bucketSize)
        {
            var batches = workItemIds.Batch(bucketSize);
            return await GetWorkItemBatchesAsync(batches);
        }

        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendQueryParam("ids", string.Join(',', workItemIds))
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");

        return await GetRootValueAsync<WorkItem>(url);
    }

    private async Task<WorkItem[]> GetWorkItemBatchesAsync(IEnumerable<int[]> batches)
    {
        var options = new ParallelOptions() { MaxDegreeOfParallelism = 8 };
        var results = new ConcurrentBag<WorkItem>();
        await Parallel.ForEachAsync(batches, options, async (batch, token) =>
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            var workItems = await GetWorkItemsAsync(batch);
            foreach (var workItem in workItems)
            {
                results.Add(workItem);
            }
        });
        return [.. results];
    }

    public async Task<WorkItem> PatchWorkItemAsync(int id, IEnumerable<WorkItemPatchItem> items) =>
        await PatchWorkItemAsync(id, items as WorkItemPatchItem[] ?? items.ToArray());

    private async Task<WorkItem> PatchWorkItemAsync(int id, WorkItemPatchItem[] items)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendPathSegment(id)
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");

        using (Logger.BeginScope("{Body}", JsonSerializer.Serialize(items)))
            Logger.LogInformation("PATCH {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);

        request.Content = new StringContent(JsonSerializer.Serialize(items), Encoding.UTF8, "application/json-patch+json");

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var workItem = await JsonSerializer.DeserializeAsync<WorkItem>(responseStream)
                   ?? throw new ApplicationException("Server did not respond");

        return workItem;
    }

    public async Task<Team[]> GetTeamsAsync()
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/teams")
            .AppendQueryParam("api-version", "6.0-preview.2");

        return await GetRootValueAsync<Team>(url);
    }

    public async Task<Iteration?> GetCurrentIterationAsync(Team team)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegments(team.ProjectName, team.Name)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendQueryParam("$timeframe", "Current")
            .AppendQueryParam("api-version", "6.1-preview");

        Iteration? iteration;
        try
        {
            iteration = (await GetRootValueAsync<Iteration>(url)).SingleOrDefault();
        }
        catch (AzureHttpRequestException ex) when (ex.TypeKey == "CurrentIterationDoesNotExistException")
        {
            return null;
        }
        if (iteration?.Attributes.FinishDate == null)
        {
            return null;
        }

        return iteration;
    }

    public async Task<WorkItemRelation[]> GetIterationWorkItemsAsync(IterationId iteration)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItems")
            .AppendQueryParam("api-version", "6.1-preview");

        var root = await GetAsync<WorkItemRelationRoot>(url);
        return root.WorkItemRelations;
    }

    public ReorderResult[] ReorderBacklogWorkItems(IterationId iteration, ReorderOperation operation)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis/work/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItemsOrder")
            .AppendQueryParam("api-version", "6.0-preview.1");
        operation.IterationPath = iteration.IterationPath;

        using (Logger.BeginScope("{ReorderOperation}", JsonSerializer.Serialize(operation)))
            Logger.LogInformation("PATCH {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);

        request.Content = new StringContent(JsonSerializer.Serialize(operation), Encoding.UTF8, "application/json");

        var response = httpClient.Send(request);
        VerifySuccessfulResponseAsync(response).Wait();

        using var responseStream = response.Content.ReadAsStream();
        var root = JsonSerializer.Deserialize<RootObject<ReorderResult>>(responseStream)
            ?? throw new ApplicationException("Server did not respond");

        return root.Value;
    }

    public async Task<Guid> GetCurrentUserIdAsync()
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/ConnectionData")
            .AppendQueryParam("api-version", "6.0-preview.1");

        var connectionData = await GetAsync<ConnectionData>(url);
        return connectionData.AuthenticatedUser.Id;
    }

    public async Task<Identity> GetIdentityAsync(Guid id)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/Identities")
            .AppendPathSegment(id)
            .AppendQueryParam("api-version", "6.0-preview.1");

        return await GetAsync<Identity>(url);
    }

    private async Task<T[]> GetRootValueAsync<T>(Url url)
    {
        var root = await GetAsync<RootObject<T>>(url);
        return root.Value;
    }

    private async Task<T> GetAsync<T>(Url url)
    {
        Logger.LogInformation("GET {Url}", url);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeader(request);

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var root = await JsonSerializer.DeserializeAsync<T>(responseStream)
                   ?? throw new ApplicationException("Server did not respond");

        return root;
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
        var cred = Convert.ToBase64String(Encoding.ASCII.GetBytes(userNamePasswordPair));
        request.Headers.Add("Authorization", $"Basic {cred}");
    }
}
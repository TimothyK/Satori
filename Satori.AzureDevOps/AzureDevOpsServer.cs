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

    /// <summary>
    /// Gets all pull requests, from all repos and all projects
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-requests/get-pull-requests?view=azure-devops-rest-6.0&tabs=HTTP
    /// </para>
    /// </remarks>
    /// <returns></returns>
    public async Task<PullRequest[]> GetPullRequestsAsync()
    {
        const int batchSize = 100;

        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
            .AppendQueryParam("$top", batchSize)
            .AppendQueryParam("api-version", "6.0");

        bool allBatchesFetched;
        var batchCount = 0;
        List<PullRequest> pullRequests = [];
        do
        {
            url.SetQueryParam("$skip", batchCount * batchSize);

            var prBatch = await GetRootValueAsync<PullRequest>(url);
            pullRequests.AddRange(prBatch);

            allBatchesFetched = prBatch.Length < batchSize;
            batchCount++;
        } while (!allBatchesFetched);

        return pullRequests.ToArray();
    }

    public async Task<PullRequest> GetPullRequestAsync(int pullRequestId)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/git/pullRequests")
            .AppendPathSegment(pullRequestId)
            .AppendQueryParam("api-version", "6.0");

        return await GetAsync<PullRequest>(url);
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

    public async Task<Tag[]> GetTagsOfMergeAsync(PullRequest pullRequest)
    {
        if (pullRequest.Status != "completed")
        {
            throw new InvalidOperationException("The pull request should be completed in order to get the Tags of the merge");
        }
        if (pullRequest.LastMergeCommit == null)
        {
            throw new InvalidOperationException($"{nameof(PullRequest.LastMergeCommit)} must have a value");
        }

        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/Contribution/HierarchyQuery/project")
            .AppendPathSegment(pullRequest.Repository.Project.Name)
            .AppendQueryParam("api-version", "5.0-preview.1");

        var payload = new ContributionHierarchyQuery
        {
            ContributionIds = [DataProviderIds.CommitsDataProvider],
            DataProviderContext = new DataProviderContext
            {
                Properties = new DataProviderContextProperties()
                {
                    RepositoryId = pullRequest.Repository.Id,
                    SearchCriteria = new SearchCriteria
                    {
                        GitArtifactsQueryArguments = new GitArtifactsQueryArguments
                        {
                            FetchTags = true,
                            CommitIds = [pullRequest.LastMergeCommit.CommitId]
                        }
                    }
                }
            }
        };

        using (Logger.BeginScope("{Body}", JsonSerializer.Serialize(payload)))
            Logger.LogInformation("POST {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeader(request);

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var dataProviderResponse = await JsonSerializer.DeserializeAsync<ContributionHierarchyResponse>(responseStream)
                       ?? throw new ApplicationException("Server did not respond");

        return dataProviderResponse.DataProviders.CommitsDataProvider?.Tags?.SelectMany(kvp => kvp.Value).ToArray() ?? [];
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

    public async Task<bool> TestPostWorkItemAsync(string projectName, IEnumerable<WorkItemPatchItem> items) =>
        await TestPostWorkItemAsync(projectName, items as WorkItemPatchItem[] ?? items.ToArray());

    private async Task<bool> TestPostWorkItemAsync(string projectName, WorkItemPatchItem[] items)
    {
        try
        {
            await PostWorkItemAsync(projectName, items, validateOnly: true);
        }
        catch (AzureHttpRequestException ex) when(ex.TypeKey == "PermissionDeniedException")
        {
            return false;
        }

        return true;
    }

    public async Task<WorkItem> PostWorkItemAsync(string projectName, IEnumerable<WorkItemPatchItem> items) =>
        await PostWorkItemAsync(projectName, items as WorkItemPatchItem[] ?? items.ToArray(), validateOnly: false);

    private async Task<WorkItem> PostWorkItemAsync(string projectName, WorkItemPatchItem[] items, bool validateOnly)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment(projectName)
            .AppendPathSegment("_apis/wit/workItems")
            .AppendPathSegment("$Task")
            .AppendQueryParam("$expand", "all")
            .AppendQueryParam("api-version", "6.0");
        if (validateOnly)
        {
            url = url.AppendQueryParam("validateOnly", true);
        }

        using (Logger.BeginScope("{Body}", JsonSerializer.Serialize(items)))
            Logger.LogInformation("POST {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        AddAuthHeader(request);

        request.Content = new StringContent(JsonSerializer.Serialize(items), Encoding.UTF8, "application/json-patch+json");

        var response = await SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var workItem = await JsonSerializer.DeserializeAsync<WorkItem>(responseStream)
                       ?? throw new ApplicationException("Server did not respond");

        return workItem;
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

        var response = await SendAsync(request);
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

    /// <summary>
    /// Return the work items linked to an iteration.
    /// </summary>
    /// <param name="iteration"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/iterations/get-iteration-work-items?view=azure-devops-rest-6.0
    /// </para>
    /// </remarks>
    public async Task<WorkItemLink[]> GetIterationWorkItemsAsync(IterationId iteration)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis")
            .AppendPathSegment("work/teamSettings/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItems")
            .AppendQueryParam("api-version", "6.1-preview");

        var root = await GetAsync<IterationWorkItems>(url);
        return root.WorkItemRelations;
    }

    public async Task<ReorderResult[]> ReorderBacklogWorkItemsAsync(IterationId iteration, ReorderOperation operation)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegments(iteration.ProjectName, iteration.TeamName)
            .AppendPathSegment("_apis/work/iterations")
            .AppendPathSegment(iteration.Id)
            .AppendPathSegment("workItemsOrder")
            .AppendQueryParam("api-version", "6.0-preview.1");
        operation.IterationPath = iteration.IterationPath;

        var payloadBody = JsonSerializer.Serialize(operation);
        using (Logger.BeginScope("{ReorderOperation}", payloadBody))
            Logger.LogInformation("PATCH {Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);

        request.Content = new StringContent(payloadBody, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
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

        var response = await SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var root = await JsonSerializer.DeserializeAsync<T>(responseStream)
                   ?? throw new ApplicationException("Server did not respond");

        return root;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        try
        {
            return await httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            throw new SecurityException($"Check network or Personal Access Token.  Failed to {request.Method} {request.RequestUri}", ex);
        }
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
        if (!ConnectionSettings.Enabled)
        {
            throw new InvalidOperationException("Azure DevOps is not enabled.  Check settings on Home page.");
        }

        var userNamePasswordPair = $":{ConnectionSettings.PersonalAccessToken}";
        var cred = Convert.ToBase64String(Encoding.ASCII.GetBytes(userNamePasswordPair));
        request.Headers.Add("Authorization", $"Basic {cred}");
    }
}
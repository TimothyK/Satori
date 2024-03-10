﻿using Flurl;
using Microsoft.Extensions.Logging;
using Pscl.CommaSeparatedValues;
using Satori.AzureDevOps.Exceptions;
using Satori.AzureDevOps.Models;
using System.Text.Json;

namespace Satori.AzureDevOps;

public class AzureDevOpsServer(
    ConnectionSettings connectionSettings
    , HttpClient httpClient
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

        return await GetRootValueAsync<PullRequest>(url);
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

        return await GetRootValueAsync<IdMap>(url);
    }

    public async Task<WorkItem[]> GetWorkItemsAsync(IEnumerable<int> workItemIds) => await GetWorkItemsAsync(workItemIds.ToArray());
    public async Task<WorkItem[]> GetWorkItemsAsync(params int[] workItemIds)
    {
        var url = ConnectionSettings.Url
            .AppendPathSegment("_apis/wit/workItems")
            .AppendQueryParam("ids", workItemIds.ToCommaSeparatedValues())
            .AppendQueryParam("api-version", "6.0");

        return await GetRootValueAsync<WorkItem>(url);
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

    public async Task<WorkItemRelation[]> GetIterationWorkItems(IterationId iteration)
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
        var cred = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(userNamePasswordPair));
        request.Headers.Add("Authorization", $"Basic {cred}");
    }
}
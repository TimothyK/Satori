﻿using Flurl;
using Microsoft.Extensions.Logging;
using Satori.Kimai.Models;
using System.Text.Json;

namespace Satori.Kimai;

public class KimaiServer(
    ConnectionSettings connectionSettings
    , HttpClient httpClient
    , ILoggerFactory loggerFactory
) : IKimaiServer
{

    private ILogger<KimaiServer> Logger => loggerFactory.CreateLogger<KimaiServer>();

    public async Task<TimeEntry[]> GetTimeSheetAsync(TimeSheetFilter filter)
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full")
            .AppendQueryParams(filter);

        return (await GetAsync<TimeEntry[]>(url));
    }

    public async Task<User> GetMyUserAsync()
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/users/me");

        return await GetAsync<User>(url);
    }

    private async Task<T> GetAsync<T>(Url url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<T>(responseStream)
                   ?? throw new ApplicationException("Server did not respond");

        return result;
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        request.Headers.Add("X-AUTH-USER", connectionSettings.UserName);
        request.Headers.Add("X-AUTH-TOKEN", connectionSettings.Token);
    }

    private static async Task VerifySuccessfulResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);
            var responseBody = await reader.ReadToEndAsync();

            throw new HttpRequestException("Bad Response: " + response.StatusCode + Environment.NewLine + responseBody, inner: null, response.StatusCode);
        }
    }

}
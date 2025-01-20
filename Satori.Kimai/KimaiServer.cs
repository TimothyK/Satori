using System.Text;
using Flurl;
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
    public bool Enabled => connectionSettings.Enabled;

    private ILogger<KimaiServer> Logger => loggerFactory.CreateLogger<KimaiServer>();

    public Uri BaseUrl => connectionSettings.Url;

    public async Task<TimeEntry[]> GetTimeSheetAsync(TimeSheetFilter filter)
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full", "true")
            .AppendQueryParams(filter);

        return await GetAsync<TimeEntry[]>(url);
    }

    public async Task<User> GetMyUserAsync()
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/users/me");

        return await GetAsync<User>(url);
    }

    public async Task ExportTimeSheetAsync(int id)
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id)
            .AppendPathSegment("export");

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);
    }

    public async Task<DateTimeOffset> StopTimerAsync(int id)
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id)
            .AppendPathSegment("stop");

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(responseStream);
        var body = await reader.ReadToEndAsync();

        try
        {
            var timeEntry = JsonSerializer.Deserialize<TimeEntryCollapsed>(body)
                   ?? throw new ApplicationException("Server did not respond");
            return timeEntry.End ?? throw new ApplicationException("Response did not define a End value");
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to deserialize {payload}", body);
            throw;
        }

    }

    public async Task UpdateTimeEntryDescriptionAsync(int id, string description)
    {        
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id);

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var payload = new Dictionary<string, object>
        {
            ["description"] = description
        };
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);
    }

    private async Task<T> GetAsync<T>(Url url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await httpClient.SendAsync(request);
        await VerifySuccessfulResponseAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(responseStream);
        var body = await reader.ReadToEndAsync();

        try
        {
            return JsonSerializer.Deserialize<T>(body)
                         ?? throw new ApplicationException("Server did not respond");
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to deserialize {payload}", body);
            throw;
        }
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        switch (connectionSettings.AuthenticationMethod)
        {
            case KimaiAuthenticationMethod.Token:
                request.Headers.Add("Authorization", "Bearer " + connectionSettings.ApiToken);
                return;
            case KimaiAuthenticationMethod.Password:
                request.Headers.Add("X-AUTH-USER", connectionSettings.UserName);
                request.Headers.Add("X-AUTH-TOKEN", connectionSettings.ApiPassword);
                return;
            default:
                throw new NotSupportedException("Unknown authentication method: " + connectionSettings.AuthenticationMethod);
        }
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
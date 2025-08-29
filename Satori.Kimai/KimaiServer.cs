using Flurl;
using Microsoft.Extensions.Logging;
using Satori.Kimai.Models;
using System.Text;
using System.Text.Json;
using Satori.TimeServices;
using Customers = Satori.Kimai.ViewModels.Customers;

namespace Satori.Kimai;

public class KimaiServer : IKimaiServer
{
    private readonly ConnectionSettings _connectionSettings;
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public KimaiServer(ConnectionSettings connectionSettings
        , HttpClient httpClient
        , ILoggerFactory loggerFactory
        , ITimeServer timeServer
    )
    {
        _connectionSettings = connectionSettings;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;

        // The customers change very rarely.
        // It is more likely that the user will reload the WASM application before the cache expires.
        _customersCache = new Cache<Customers>(FetchCustomersAsync, timeServer)
        {
            MaxAge = TimeSpan.FromHours(1)
        };
    }

    public bool Enabled => _connectionSettings.Enabled;

    private ILogger<KimaiServer> Logger => _loggerFactory.CreateLogger<KimaiServer>();

    public Uri BaseUrl => _connectionSettings.Url;

    public async Task<TimeEntry[]> GetTimeSheetAsync(TimeSheetFilter filter)
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full", "true")
            .AppendQueryParams(filter);

        return await GetAsync<TimeEntry[]>(url);
    }

    public async Task<User> GetMyUserAsync()
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/users/me");

        return await GetAsync<User>(url);
    }

    public async Task ExportTimeSheetAsync(int id)
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id)
            .AppendPathSegment("export");

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await SendAsync(request);
        await VerifySuccessfulResponseAsync(response);
    }

    public async Task<DateTimeOffset> StopTimerAsync(int id)
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id)
            .AppendPathSegment("stop");

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await SendAsync(request);
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
        var url = _connectionSettings.Url
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

        var response = await SendAsync(request);
        await VerifySuccessfulResponseAsync(response);
    }

    public async Task<TimeEntry> CreateTimeEntryAsync(TimeEntryForCreate entry)
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full", "true");

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        var payload = JsonSerializer.Serialize(entry);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return await SendAsync<TimeEntry>(request);
    }

    public Task<TimeEntryCollapsed> GetTimeEntryAsync(int id)
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id);

        return GetAsync<TimeEntryCollapsed>(url);
    }

    #region GetCustomers

    private readonly Cache<Customers> _customersCache;

    public async Task<Customers> GetCustomersAsync()
    {
        return await _customersCache.GetValueAsync();
    }

    private async Task<Customers> FetchCustomersAsync()
    {
        var customerMasters = await GetCustomerMastersAsync();
        var customers = new Customers(customerMasters);

        var projects = await GetProjectMastersAsync();
        foreach (var project in projects)
        {
            customers.Add(project);
        }

        var activities = await GetActivityMastersAsync();
        foreach (var activity in activities)
        {
            customers.Add(activity);
        }

        return customers;
    }

    private async Task<Customer[]> GetCustomerMastersAsync()
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/customers")
            .AppendQueryParam("visible", 1);

        var customers = await GetAsync<Customer[]>(url);
        return customers;
    }

    private async Task<ProjectMaster[]> GetProjectMastersAsync()
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/projects")
            .AppendQueryParam("visible", 1);

        var projects = await GetAsync<ProjectMaster[]>(url);
        return projects;
    }

    private async Task<ActivityMaster[]> GetActivityMastersAsync()
    {
        var url = _connectionSettings.Url
            .AppendPathSegment("api/activities")
            .AppendQueryParam("visible", 1);

        var activities = await GetAsync<ActivityMaster[]>(url);
        return activities;
    }

    #endregion GetCustomers

    #region Common HTTP Methods

    private async Task<T> GetAsync<T>(Url url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<T>(request);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        AddAuthHeader(request);
        Logger.LogInformation("{Method} {Url}", request.Method.ToString().ToUpper(), request.RequestUri);

        var response = await SendAsync(request);
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

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            throw new ApplicationException($"Check network.  Failed to {request.Method} {request.RequestUri}", ex);
        }
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (!Enabled)
        {
            throw new InvalidOperationException("Kimai is disabled");
        }

        switch (_connectionSettings.AuthenticationMethod)
        {
            case KimaiAuthenticationMethod.Token:
                request.Headers.Add("Authorization", "Bearer " + _connectionSettings.ApiToken);
                return;
            case KimaiAuthenticationMethod.Password:
                request.Headers.Add("X-AUTH-USER", _connectionSettings.UserName);
                request.Headers.Add("X-AUTH-TOKEN", _connectionSettings.ApiPassword);
                return;
            default:
                throw new NotSupportedException("Unknown authentication method: " + _connectionSettings.AuthenticationMethod);
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

            throw new HttpRequestException($"Bad Response from {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {response.StatusCode}{Environment.NewLine}{responseBody}", inner: null, response.StatusCode);
        }
    }

    #endregion
}
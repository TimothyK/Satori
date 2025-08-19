using System.Text;
using Flurl;
using Microsoft.Extensions.Logging;
using Satori.Kimai.Models;
using System.Text.Json;
using Satori.Kimai.Utilities;
using CustomerViewModel = Satori.Kimai.ViewModels.Customer;
using ProjectViewModel = Satori.Kimai.ViewModels.Project;

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

        var response = await SendAsync(request);
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

        var response = await SendAsync(request);
        await VerifySuccessfulResponseAsync(response);
    }

    public async Task<TimeEntry> CreateTimeEntryAsync(TimeEntryForCreate entry)
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full", "true");

        var request = new HttpRequestMessage(HttpMethod.Post, url);

        var payload = JsonSerializer.Serialize(entry);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return await SendAsync<TimeEntry>(request);
    }

    public Task<TimeEntryCollapsed> GetTimeEntryAsync(int id)
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendPathSegment(id);

        return GetAsync<TimeEntryCollapsed>(url);
    }

    #region GetCustomers

    private static Task<CustomerViewModel[]>? _cachedCustomersTask;

    public void ResetCustomerCache()
    {
        _cachedCustomersTask = null;
    }

    public async Task<CustomerViewModel[]> GetCustomersAsync()
    {
        // If a fetch is already in progress or completed, await it
        var task = _cachedCustomersTask;
        if (task != null)
        {
            return await task;
        }

        // Only one thread should set _cachedCustomersTask
        var newTask = FetchCustomersAsync();
        var originalTask = Interlocked.CompareExchange(ref _cachedCustomersTask, newTask, null);
        if (originalTask != null)
        {
            return await originalTask;
        }

        return await newTask;
    }

    private async Task<CustomerViewModel[]> FetchCustomersAsync()
    {
        var customerMasters = await GetCustomerMastersAsync();
        var projects = await GetProjectMastersAsync();

        var customers = customerMasters.Select(AddToViewModel).ToArray();

        foreach (var project in projects)
        {
            var customer = customers.FirstOrDefault(c => c.Id == project.Customer);
            if (customer != null)
            {
                AddToViewModel(project, customer);
            }
        }

        var activities = await GetActivityMastersAsync();
        foreach (var activity in activities)
        {
            var project = customers
                .SelectMany(customer => customer.Projects)
                .FirstOrDefault(p => p.Id == activity.Project);
            if (project != null)
            {
                AddToViewModel(activity, project);
            }
        }

        return customers;
    }

    private static void AddToViewModel(ProjectMaster project, CustomerViewModel customer)
    {
        var projectViewModel = new ProjectViewModel
        {
            Id = project.Id,
            Name = project.Name,
            ProjectCode = ProjectCodeParser.GetProjectCode(project.Name),
            Customer = customer
        };
        customer.Projects.Add(projectViewModel);
    }

    private static void AddToViewModel(ActivityMaster activity, ProjectViewModel project)
    {
        var activityViewModel = new ViewModels.Activity
        {
            Id = activity.Id,
            Name = activity.Name,
            ActivityCode = ProjectCodeParser.GetActivityCode(activity.Name),
            Project = project
        };
        project.Activities.Add(activityViewModel);
    }

    private async Task<Customer[]> GetCustomerMastersAsync()
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/customers")
            .AppendQueryParam("visible", 1);

        var customers = await GetAsync<Customer[]>(url);
        return customers;
    }
    
    private async Task<ProjectMaster[]> GetProjectMastersAsync()
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/projects")
            .AppendQueryParam("visible", 1);

        var projects = await GetAsync<ProjectMaster[]>(url);
        return projects;
    }
    
    private async Task<ActivityMaster[]> GetActivityMastersAsync()
    {
        var url = connectionSettings.Url
            .AppendPathSegment("api/activities")
            .AppendQueryParam("visible", 1);

        var activities = await GetAsync<ActivityMaster[]>(url);
        return activities;
    }

    private static CustomerViewModel AddToViewModel(Customer dto)
    {
        return new CustomerViewModel()
        {
            Id = dto.Id,
            Name = dto.Name,
            Logo = CustomerLogoParser.GetCustomerLogo(dto.Comment)
        };
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
            return await httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            throw new ApplicationException($"Check network.  Failed to {request.Method} {request.RequestUri}", ex);
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

            throw new HttpRequestException($"Bad Response from {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {response.StatusCode}{Environment.NewLine}{responseBody}", inner: null, response.StatusCode);
        }
    }

    #endregion
}
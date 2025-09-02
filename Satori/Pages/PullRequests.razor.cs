using CodeMonkeyProjectiles.Linq;
using Flurl;
using Microsoft.JSInterop;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.ViewModels;
using Satori.Pages.Components;
using Satori.Utilities;
using Toolbelt.Blazor.HotKeys2;

namespace Satori.Pages;

public partial class PullRequests
{
    private PullRequest[] _pullRequests = [];

    private IReadOnlyCollection<PullRequest> FilteredPullRequests { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if (!ConnectionSettingsStore.GetAzureDevOpsSettings().Enabled)
        {
            // This page shouldn't be accessible if AzureDevOps is disabled.  Go to Home page where AzureDevOps can be configured/enabled.
            NavigationManager.NavigateTo("/");
        }

        ForFilterInitializeFromUrl();
        PersonFilterInitializedFromUrl(AuthorFilter, AuthorQueryParamName);
        PersonFilterInitializedFromUrl(WithPersonFilter, WithQueryParamName);
        PersonFilterInitializedFromUrl(ActionItemPersonFilter, ActionItemQueryParamName);

        await RefreshAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HotKeys.CreateContext()
                .Add(ModCode.Alt, Code.F5, RefreshAsync, new HotKeyOptions { Description = "Refresh" });
        }

        await SetDefaultForFilterAsync();
        await SetDefaultPersonFilterAsync(AuthorFilter, DefaultAuthorFilterStorageKey, AuthorQueryParamName);
        await SetDefaultPersonFilterAsync(WithPersonFilter, DefaultWithFilterStorageKey, WithQueryParamName);
        await SetDefaultPersonFilterAsync(ActionItemPersonFilter, DefaultActionItemFilterStorageKey, ActionItemQueryParamName);

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task RefreshAsync()
    {
        InLoading = InLoadingCssClass;
        StateHasChanged();

        _pullRequests = (await PullRequestService.GetPullRequestsAsync()).ToArray();
        SetFilteredPullRequests();
        StateHasChanged();  //Quickly show the PR list to the user.

        const int pageSize = 20;
        var topPrs = _pullRequests.Take(pageSize).ToArray();
        await PullRequestService.AddWorkItemsToPullRequestsAsync(topPrs);
        SetFilteredPullRequests();
        StateHasChanged();

        await PullRequestService.AddWorkItemsToPullRequestsAsync(_pullRequests.Skip(pageSize).ToArray());
        SetFilteredPullRequests();

        InLoading = CssClass.None;
    }

    private void SetFilteredPullRequests()
    {
        Projects = _pullRequests
            .SelectMany(pr => pr.WorkItems)
            .Select(workItem => workItem.KimaiProject)
            .Where(project => project != null).Select(p => p!)
            .Distinct()
            .ToArray();

        Authors = _pullRequests
            .Select(pr => pr.CreatedBy)
            .Distinct()
            .ToArray();

        WithPeople = _pullRequests.Select(pr => pr.CreatedBy)
            .Union(_pullRequests.SelectMany(pr => pr.Reviews).Where(review => !review.HasDeclined).Select(review => review.Reviewer))
            .Distinct()
            .ToArray();

        _pullRequests.ResetActionItems();
        ActionItemPeople = _pullRequests
            .SelectMany(pr => pr.ActionItems)
            .SelectMany(actionItem => actionItem.On)
            .Select(assignment => assignment.Person)
            .Distinct()
            .ToArray();

        FilteredPullRequests = _pullRequests
            .Where(MatchesForFilter)
            .Where(MatchesAuthorFilter)
            .Where(MatchesWithFilter)
            .Where(MatchesActionItemFilter)
            .OrderBy(pr =>
                pr.WorkItems.Select(workItem => workItem.AbsolutePriority)
                    .DefaultIfEmpty(double.MaxValue)
                    .Min())
            .ThenByDescending(pr => pr.Id)
            .ToArray();
    }

    private bool MatchesForFilter(PullRequest pr)
    {
        if (ForFilter.FilterKey == "all")
        {
            return true;
        }
        if (ForFilter.FilterKey == "?")
        {
            return pr.WorkItems.All(workItem => workItem.KimaiProject == null);
        }

        if (ForFilter.CurrentProject != null)
        {
            return pr.WorkItems.Any(workItem => workItem.KimaiProject == ForFilter.CurrentProject);
        }
        if (ForFilter.CurrentCustomer != null)
        {
            return pr.WorkItems.Any(workItem => workItem.KimaiProject?.Customer == ForFilter.CurrentCustomer);
        }
        return true;
    }

    private bool MatchesAuthorFilter(PullRequest pr)
    {
        return AuthorFilter.CurrentPerson == Person.Anyone
               || pr.CreatedBy == AuthorFilter.CurrentPerson;
    }

    private bool MatchesWithFilter(PullRequest pr)
    {
        return WithPersonFilter.CurrentPerson == Person.Anyone
               || WithPersonFilter.CurrentPerson.IsIn(
                   pr.CreatedBy.Yield()
                   .Union(pr.Reviews.Where(review => !review.HasDeclined).Select(review => review.Reviewer)));
    }

    private bool MatchesActionItemFilter(PullRequest pr)
    {
        return ActionItemPersonFilter.CurrentPerson == Person.Anyone
               || ActionItemPersonFilter.CurrentPerson.IsIn(pr.ActionItems.SelectMany(actionItem => actionItem.On).Select(assignment => assignment.Person));
    }

    private static readonly CssClass InLoadingCssClass = new("in-loading");
    private CssClass InLoading { get; set; } = InLoadingCssClass;


    #region Cell Links

    private async Task OpenPullRequestAsync(PullRequest pullRequest)
    {
        await JsRuntime.InvokeVoidAsync("open", pullRequest.Url, "_blank");
    }
    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

    #endregion Cell Links

    #region ForFilter

    private const string DefaultForFilterStorageKey = "PullRequest.For";
    private const string ForQueryParamName = "for";

    public required CustomerFilter ForFilter { get; set; }

    private IReadOnlyCollection<Project> Projects { get; set; } = [];

    private async Task SetDefaultForFilterAsync()
    {
        var hasForFilterOnUrl = new Url(NavigationManager.Uri).QueryParams.Any(qp => qp.Name == ForQueryParamName);
        if (hasForFilterOnUrl)
        {
            return;
        }

        var filterValue = await LocalStorage.GetItemAsync<string>(DefaultForFilterStorageKey) ?? "all";
        ForFilter.FilterKey = filterValue;
    }

    private async Task OnForFilterChangedAsync()
    {
        SetFilteredPullRequests();
        ResetForOnUrl();
        await StoreFilterAsync(DefaultForFilterStorageKey, ForFilter.FilterKey);
    }

    private void ResetForOnUrl()
    {
        var filterValue = ForFilter.FilterKey;

        var url = NavigationManager.Uri
            .RemoveQueryParam(ForQueryParamName)
            .AppendQueryParam(ForQueryParamName, filterValue);

        NavigationManager.NavigateTo(url, forceLoad: false);
    }

    private void ForFilterInitializeFromUrl()
    {
        var parameters = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == ForQueryParamName)
            .ToArray();
        if (parameters.None())
        {
            return;
        }
        var filterValue = parameters.First().Value.ToString() ?? "all";
        ForFilter.FilterKey = filterValue;
    }


    #endregion ForFilter

    #region Author (By) Filter

    private const string DefaultAuthorFilterStorageKey = "PullRequest.By";
    private const string AuthorQueryParamName = "by";

    public required PersonFilter AuthorFilter { get; set; }

    private IReadOnlyCollection<Person> Authors { get; set; } = [];

    private async Task OnAuthorFilterChangedAsync()
    {
        SetFilteredPullRequests();
        ResetPersonOnUrl(AuthorFilter, AuthorQueryParamName);
        await StoreFilterAsync(DefaultAuthorFilterStorageKey, AuthorFilter.FilterKey);
    }

    private void ResetPersonOnUrl(PersonFilter filter, string queryParamName)
    {
        var filterValue = filter.FilterKey;

        var url = NavigationManager.Uri
            .RemoveQueryParam(queryParamName)
            .AppendQueryParam(queryParamName, filterValue);

        NavigationManager.NavigateTo(url, forceLoad: false);
    }

    private async Task StoreFilterAsync(string storageKey, string filterValue)
    {
        if (LocalStorage == null)
        {
            return;
        }

        await LocalStorage.SetItemAsync(storageKey, filterValue);
    }

    private void PersonFilterInitializedFromUrl(PersonFilter filter, string paramName)
    {
        var parameters = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == paramName)
            .ToArray();
        if (parameters.None())
        {
            return;
        }
        var filterValue = parameters.First().Value.ToString() ?? "all";
        filter.FilterKey = filterValue;
    }

    private async Task SetDefaultPersonFilterAsync(PersonFilter filter, string storageKey, string queryParamName)
    {
        var hasForFilterOnUrl = new Url(NavigationManager.Uri).QueryParams.Any(qp => qp.Name == queryParamName);
        if (hasForFilterOnUrl)
        {
            return;
        }

        var filterValue = await LocalStorage.GetItemAsync<string>(storageKey) ?? "all";
        filter.FilterKey = filterValue;
    }

    #endregion Author (By) Filter

    #region With Filter

    private const string DefaultWithFilterStorageKey = "PullRequest.With";
    private const string WithQueryParamName = "with";

    public required PersonFilter WithPersonFilter { get; set; }
    private IReadOnlyCollection<Person> WithPeople { get; set; } = [];

    private async Task OnWithFilterChangedAsync()
    {
        SetFilteredPullRequests();
        ResetPersonOnUrl(WithPersonFilter, WithQueryParamName);
        await StoreFilterAsync(DefaultWithFilterStorageKey, WithPersonFilter.FilterKey);

    }

    #endregion With Filter

    #region Action Items (On) Filter

    private const string DefaultActionItemFilterStorageKey = "PullRequest.On";
    private const string ActionItemQueryParamName = "on";

    public required PersonFilter ActionItemPersonFilter { get; set; }
    private IReadOnlyCollection<Person> ActionItemPeople { get; set; } = [];

    private async Task OnActionItemFilterChangedAsync()
    {
        SetFilteredPullRequests();
        ResetPersonOnUrl(ActionItemPersonFilter, ActionItemQueryParamName);
        await StoreFilterAsync(DefaultActionItemFilterStorageKey, ActionItemPersonFilter.FilterKey);
    }

    #endregion Action Items (On) Filter
}
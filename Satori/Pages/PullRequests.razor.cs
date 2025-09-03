using CodeMonkeyProjectiles.Linq;
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

        await RefreshAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HotKeys.CreateContext()
                .Add(ModCode.Alt, Code.F5, RefreshAsync, new HotKeyOptions { Description = "Refresh" });
        }

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

    public required CustomerFilter ForFilter { get; set; }

    private IReadOnlyCollection<Project> Projects { get; set; } = [];

    private void OnForFilterChanged()
    {
        SetFilteredPullRequests();
    }

    #endregion ForFilter

    #region Author (By) Filter

    public required PersonFilter AuthorFilter { get; set; }

    private IReadOnlyCollection<Person> Authors { get; set; } = [];

    private void OnAuthorFilterChanged()
    {
        SetFilteredPullRequests();
    }

    #endregion Author (By) Filter

    #region With Filter

    public required PersonFilter WithPersonFilter { get; set; }
    private IReadOnlyCollection<Person> WithPeople { get; set; } = [];

    private void OnWithFilterChanged()
    {
        SetFilteredPullRequests();
    }

    #endregion With Filter

    #region Action Items (On) Filter

    public required PersonFilter ActionItemPersonFilter { get; set; }
    private IReadOnlyCollection<Person> ActionItemPeople { get; set; } = [];

    private void OnActionItemFilterChanged()
    {
        SetFilteredPullRequests();
    }

    #endregion Action Items (On) Filter
}
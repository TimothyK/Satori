using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Utilities;
using Toolbelt.Blazor.HotKeys2;

namespace Satori.Pages;

public partial class PullRequests
{
    private PullRequest[]? _pullRequests;

    protected override async Task OnInitializedAsync()
    {
        if (!ConnectionSettingsStore.GetAzureDevOpsSettings().Enabled)
        {
            // This page shouldn't be accessible if Kimai is disabled.  Go to Home page where AzureDevOps can be configured/enabled.
            NavigationManager.NavigateTo("/");
        }

        await RefreshAsync();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HotKeys.CreateContext()
                .Add(ModCode.Alt, Code.F5, RefreshAsync, new HotKeyOptions { Description = "Refresh" });
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    private async Task RefreshAsync()
    {
        InLoading = InLoadingCssClass;
        StateHasChanged();

        _pullRequests = (await PullRequestService.GetPullRequestsAsync()).ToArray();
        StateHasChanged();  //Quickly show the PR list to the user.

        const int pageSize = 20;
        var topPrs = _pullRequests.Take(pageSize).ToArray();
        await PullRequestService.AddWorkItemsToPullRequestsAsync(topPrs);
        StateHasChanged();

        await PullRequestService.AddWorkItemsToPullRequestsAsync(_pullRequests.Skip(pageSize).ToArray());

        InLoading = CssClass.None;
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
}
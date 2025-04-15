using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.PullRequests;

namespace Satori.Pages.SprintBoard;

public partial class PullRequestView
{
    [Parameter]
    public required PullRequest PullRequest { get; set; }

    private async Task OpenPullRequestAsync(PullRequest pullRequest)
    {
        await JsRuntime.InvokeVoidAsync("open", pullRequest.Url, "_blank");
    }
}
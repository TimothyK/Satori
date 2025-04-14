using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels.PullRequests;

namespace Satori.Pages.SprintBoard;

public partial class PullRequestView
{
    [Parameter]
    public required PullRequest PullRequest { get; set; }
}
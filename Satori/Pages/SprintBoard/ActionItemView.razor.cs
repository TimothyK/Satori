using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels.Abstractions;

namespace Satori.Pages.SprintBoard;

public partial class ActionItemView
{
    [Parameter]
    public required ActionItem ActionItem { get; set; }
}

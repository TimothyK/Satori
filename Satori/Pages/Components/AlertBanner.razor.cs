using Satori.AppServices.Services;
using Satori.AppServices.ViewModels.AlertServices;

namespace Satori.Pages.Components;

public partial class AlertBanner
{
    protected override void OnInitialized()
    {
        base.OnInitialized();

        ViewModel = AlertService.Subscribe();
        ViewModel.Changed += (_, _) => StateHasChanged();
    }

    public required AlertViewModel ViewModel { get; set; }
}
using Satori.AppServices.ViewModels.AlertServices;

namespace Satori.AppServices.Services.Abstractions;

public interface IAlertService
{
    void BroadcastAlert(Exception ex);
    void BroadcastAlert(string message);
    void ClearAlert();
    AlertViewModel Subscribe();
}
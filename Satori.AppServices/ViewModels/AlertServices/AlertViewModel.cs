namespace Satori.AppServices.ViewModels.AlertServices;

public class AlertViewModel
{
    public string Message { get; private set; } = string.Empty;
    public AlertLevel AlertLevel { get; private set; } = AlertLevel.Warning;
    public bool Visible { get; private set; }

    public void ShowAlert(string message, AlertLevel level)
    {
        Message = message;
        AlertLevel = level;
        Visible = true;
        OnChanged();
    }
    public void ClearAlert()
    {
        Message = string.Empty;
        Visible = false;
        OnChanged();
    }

    public event EventHandler? Changed;

    private void OnChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
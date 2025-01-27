using System.Timers;
using Timer = System.Timers.Timer;

namespace Satori.AppServices.Services;

public class AlertService
{
    #region BroadcastAlert

    public void BroadcastAlert(Exception ex)
    {
        Console.WriteLine(ex);

        _alertViewModel.ShowAlert(ex.Message);
        StartAutoCloseTimer();
    }

    #endregion BroadcastAlert

    #region Auto Close Timer

    private Timer? _autoCloseTimer;

    private void StartAutoCloseTimer()
    {
        StopAutoCloseTimer();

        _autoCloseTimer = new Timer(TimeSpan.FromSeconds(30));
        _autoCloseTimer.Elapsed += AutoClose;
        _autoCloseTimer.Start();
    }

    private void AutoClose(object? sender, ElapsedEventArgs e)
    {
        _alertViewModel.ClearAlert();

        StopAutoCloseTimer();
    }

    private void StopAutoCloseTimer()
    {
        if (_autoCloseTimer == null)
        {
            return;
        }

        _autoCloseTimer.Stop();
        _autoCloseTimer.Elapsed -= AutoClose;
        _autoCloseTimer = null;
    }

    #endregion Auto Close Timer

    #region Subscribe

    private readonly AlertViewModel _alertViewModel = new();

    public AlertViewModel Subscribe()
    {
        return _alertViewModel;
    }

    #endregion Subscribe

}


public class AlertViewModel
{
    public string Message { get; private set; } = string.Empty;
    public bool Visible { get; private set; }

    public void ShowAlert(string message)
    {
        Message = message;
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
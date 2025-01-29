using System.Timers;
using Timer = System.Timers.Timer;

namespace Satori.AppServices.Services;

/// <summary>
/// This is a Singleton service that stores globally an error message to show to the user.
/// </summary>
/// <remarks>
/// <para>
/// Any consumer can <see cref="BroadcastAlert(Exception)"/> if they catch an exception.
/// They can <see cref="ClearAlert"/> before trying to invoke an action so that it is clear that if an error occurred it was from that button click.
/// </para>
/// <para>
/// Only the view that shows the alert should <see cref="Subscribe"/>
/// </para>
/// </remarks>
public class AlertService
{
    #region BroadcastAlert

    public void BroadcastAlert(Exception ex)
    {
        Console.WriteLine(ex);

        _alertViewModel.ShowAlert(ex.Message, AlertLevel.Error);
        StartAutoCloseTimer();
    }

    public void BroadcastAlert(string message)
    {
        Console.WriteLine("Showing alert: " + message);

        _alertViewModel.ShowAlert(message, AlertLevel.Warning);
        StartAutoCloseTimer();
    }

    public void ClearAlert()
    {
        StopAutoCloseTimer();
        _alertViewModel.ClearAlert();
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

public class AlertLevel
{
    private readonly string _cssClassName;

    private AlertLevel(string cssClassName)
    {
        _cssClassName = cssClassName;
    }

    public static readonly AlertLevel Error = new("alert-error");
    public static readonly AlertLevel Warning = new("alert-warning");

    public override string ToString()
    {
        return _cssClassName;
    }
}
namespace Satori.AppServices.ViewModels.AlertServices;

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
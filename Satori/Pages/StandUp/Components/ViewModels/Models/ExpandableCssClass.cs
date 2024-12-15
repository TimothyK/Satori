using Satori.Utilities;

namespace Satori.Pages.StandUp.Components.ViewModels.Models;

public class ExpandableCssClass : CssClass
{
    private ExpandableCssClass(string className) : base(className)
    {
    }
    
    public static ExpandableCssClass Expanded { get; } = new("expanded");
    public static ExpandableCssClass Collapsed { get; } = new("collapsed");
    
    public ExpandableCssClass Not => this == Expanded ? Collapsed : Expanded;
    
    public static implicit operator bool(ExpandableCssClass cssClass) => cssClass == Expanded;
    public static implicit operator ExpandableCssClass(bool value) => value ? Expanded : Collapsed;

}
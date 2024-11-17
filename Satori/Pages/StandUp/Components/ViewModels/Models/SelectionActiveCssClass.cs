using Satori.Utilities;

namespace Satori.Pages.StandUp.Components.ViewModels.Models;

public class SelectionActiveCssClass : CssClass
{
    private SelectionActiveCssClass(string className) : base(className)
    {
    }

    public static SelectionActiveCssClass Activated { get; } = new("selection-activated");
    public static SelectionActiveCssClass Deactivated { get; } = new("selection-deactivated");
    public static SelectionActiveCssClass ToBeDeleted { get; } = new("selection-delete");

    public SelectionActiveCssClass Not => this == Activated ? Deactivated : Activated;

    public static implicit operator bool(SelectionActiveCssClass cssClass) => cssClass == Activated;
    public static implicit operator SelectionActiveCssClass(bool value) => value ? Activated : Deactivated;
}
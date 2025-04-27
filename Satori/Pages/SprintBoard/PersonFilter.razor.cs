using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;

namespace Satori.Pages.SprintBoard;

public partial class PersonFilter
{
    [Parameter]
    public required Person[] People { get; set; }

    private Person CurrentPerson { get; set; } = Person.Anyone;
    private FilterSelectionCssClass FilterWithBorders { get; set; } = FilterSelectionCssClass.Hidden;

    private void SetFilter(Person person)
    {
        CurrentPerson = person;
        FilterWithBorders = person == Person.Anyone ? FilterSelectionCssClass.Hidden : FilterSelectionCssClass.Selected;
    }

    private void SetFilterMe()
    {
        if (Person.Me == null)
        {
            throw new InvalidOperationException("Me is not defined");
        }
        SetFilter(Person.Me);
    }
}
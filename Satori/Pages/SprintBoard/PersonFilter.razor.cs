using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;

namespace Satori.Pages.SprintBoard;

public partial class PersonFilter
{
    private Person[] _people;

    [Parameter]
#pragma warning disable BL0007
    public required IEnumerable<Person> People
#pragma warning restore BL0007
    {
        get => _people;
        [MemberNotNull(nameof(_people))]
        set
        {
            _people = value.Distinct().OrderBy(p => p.DisplayName).ToArray();
            FilterKey = _filterKey;
        }
    }

    [Parameter]
    public EventCallback OnFilterChanged { get; set; }


    public Person CurrentPerson { get; private set; } = Person.Anyone;
    private FilterSelectionCssClass FilterWithBorders { get; set; } = FilterSelectionCssClass.Hidden;

    private bool IsMeFilter { get; set; }

    private string _filterKey = "all";
    public string FilterKey
    {
        get =>
            IsMeFilter ? "me"
            : CurrentPerson == Person.Anyone ? "all"
            : CurrentPerson == Person.Empty ? "?"
            : CurrentPerson.AzureDevOpsId.ToString();
        set
        {
            _filterKey = value;
            IsMeFilter = value == "me";
            switch (value)
            {
                case "me":
                    CurrentPerson = Person.Me ?? Person.Anyone;
                    break;
                case "all":
                    CurrentPerson = Person.Anyone;
                    break;
                case "?":
                    CurrentPerson = Person.Empty;
                    break;
                default:
                {
                    if (Guid.TryParse(value, out var id))
                    {
                        CurrentPerson = People.FirstOrDefault(p => p.AzureDevOpsId == id) ?? Person.Anyone;
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid filter key: {value}");
                    }

                    break;
                }
            }
            FilterWithBorders = CurrentPerson == Person.Anyone ? FilterSelectionCssClass.Hidden : FilterSelectionCssClass.Selected;
        }
    }

    private async Task SetFilterAsync(Person person)
    {
        await SetFilterAsync(person, isMe: false);
    }

    private async Task SetFilterMeAsync()
    {
        if (Person.Me == null)
        {
            throw new InvalidOperationException("Me is not defined");
        }
        await SetFilterAsync(Person.Me, isMe: true);
    }

    private async Task SetFilterAsync(Person person, bool isMe)
    {
        FilterKey = isMe ? "me" 
            : person == Person.Anyone ? "all"
            : person == Person.Empty ? "?"
            : person.AzureDevOpsId.ToString();

        await OnFilterChanged.InvokeAsync();
    }

    private async Task ToggleFilterAsync()
    {
        if (CurrentPerson == Person.Anyone && Person.Me != null)
        {
            await SetFilterAsync(Person.Me, isMe: true);
        }
        else
        {
            await SetFilterAsync(Person.Anyone);
        }
    }
}
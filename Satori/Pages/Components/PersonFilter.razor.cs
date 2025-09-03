using Flurl;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;
using Satori.Pages.SprintBoard;

namespace Satori.Pages.Components;

public partial class PersonFilter
{
    [Parameter]
    public required string Label { get; set; }

    [Parameter]
    public required IEnumerable<Person> People { get; set; }

    [Parameter]
    public EventCallback OnFilterChanged { get; set; }

    [Parameter] public bool AllowNull { get; set; } = true;

    [Parameter] public required string QueryParamName { get; set; }
    [Parameter] public required string StorageKeyName { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var parameters = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == QueryParamName)
            .ToArray();
        if (parameters.Any())
        {
            FilterKey = parameters.First().Value.ToString() ?? "all";
        }
        
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await SetDefaultPersonFilterAsync();

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task SetDefaultPersonFilterAsync()
    {
        var hasFilterOnUrl = new Url(NavigationManager.Uri).QueryParams.Any(qp => qp.Name == QueryParamName);
        if (hasFilterOnUrl)
        {
            return;
        }

        var filterValue = await LocalStorage.GetItemAsync<string>(StorageKeyName) ?? "all";
        FilterKey = filterValue;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        People = People.Distinct().OrderBy(p => p.DisplayName).ToArray();

        //Reset the filter now that the available People is set.
        FilterKey = _filterKey;
    }

    public Person CurrentPerson { get; private set; } = Person.Anyone;
    private FilterSelectionCssClass FilterWithBorders { get; set; } = FilterSelectionCssClass.Hidden;

    private bool IsMeFilter { get; set; }

    private string _filterKey = "all";
    public string FilterKey
    {
        get => _filterKey;
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

    private async Task OnSetFilterAsync(Person person)
    {
        await OnSetFilterAsync(person, isMe: false);
    }

    private async Task SetFilterMeAsync()
    {
        if (Person.Me == null)
        {
            throw new InvalidOperationException("Me is not defined");
        }
        await OnSetFilterAsync(Person.Me, isMe: true);
    }

    private async Task OnSetFilterAsync(Person person, bool isMe)
    {
        FilterKey = isMe ? "me" 
            : person == Person.Anyone ? "all"
            : person == Person.Empty ? "?"
            : person.AzureDevOpsId.ToString();

        ResetPersonOnUrl();
        await StoreFilterAsync();

        await OnFilterChanged.InvokeAsync();
    }

    private void ResetPersonOnUrl()
    {
        var filterValue = FilterKey;

        var url = NavigationManager.Uri
            .RemoveQueryParam(QueryParamName)
            .AppendQueryParam(QueryParamName, filterValue);

        NavigationManager.NavigateTo(url, forceLoad: false);
    }

    private async Task StoreFilterAsync()
    {
        if (LocalStorage == null)
        {
            return;
        }

        await LocalStorage.SetItemAsync(StorageKeyName, FilterKey);
    }

    private async Task ToggleFilterAsync()
    {
        if (CurrentPerson == Person.Anyone && Person.Me != null)
        {
            await OnSetFilterAsync(Person.Me, isMe: true);
        }
        else
        {
            await OnSetFilterAsync(Person.Anyone);
        }
    }
}
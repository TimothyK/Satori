using Flurl;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;
using Satori.Kimai.ViewModels;
using Satori.Pages.SprintBoard;

namespace Satori.Pages.Components;

public partial class CustomerFilter
{
    private Customer[] _customers = [];

    [Parameter]
    public required IReadOnlyCollection<Project> Projects { get; set; }

    [Parameter]
    public EventCallback OnFilterChanged { get; set; }

    [Parameter] public required string QueryParamName { get; set; }
    [Parameter] public required string StorageKeyName { get; set; }

    protected override Task OnInitializedAsync()
    {
        var parameters = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == QueryParamName)
            .ToArray();
        if (parameters.Any())
        {
            FilterKey = parameters.First().Value.ToString() ?? "all";
        }

        return base.OnInitializedAsync();
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

        _customers = Projects
            .Select(p => p.Customer)
            .Distinct()
            .OrderBy(customer => customer.Name)
            .ToArray();

        //Reset the filter now that the available projects and customers is set.
        FilterKey = _filterKey;  
    }

    public Customer? CurrentCustomer { get; set; }
    public Customer? LastCustomer { get; set; }
    public Project? CurrentProject { get; set; }

    public Uri CurrentCustomerLogo =>
        FilterKey switch
        {
            "all" => Person.Anyone.AvatarUrl,
            "?" => Person.Empty.AvatarUrl,
            _ => CurrentCustomer == null ? Person.Anyone.AvatarUrl // Invalid filter, can occur during loading.  Treat as "all"
                : CurrentCustomer.Logo
        };
    public string CurrentCustomerDisplayName =>
        FilterKey switch
        {
            "all" => "Any",
            "?" => "Unknown",
            _ => CurrentCustomer?.Name ?? "Any"
        };

    private string _filterKey = "all";

    public string FilterKey
    {
        get => _filterKey;
        set
        {
            _filterKey = value;
            if (value is "all" or "?")
            {
                CurrentCustomer = null;
                CurrentProject = null;
            }
            else
            {
                var customer = _customers.FirstOrDefault(c => c.Acronym == value);
                if (customer != null)
                {
                    CurrentCustomer = customer;
                    CurrentProject = null;
                    LastCustomer = CurrentCustomer;
                }
                else
                {
                    var project = Projects.FirstOrDefault(p => p.ProjectCode == value);
                    if (project != null)
                    {
                        CurrentProject = project;
                        CurrentCustomer = project.Customer;
                        LastCustomer = CurrentCustomer;
                    }
                }
            }
            FilterBorders = CurrentCustomer == null ? FilterSelectionCssClass.Hidden : FilterSelectionCssClass.Selected;
        }
    }

    private FilterSelectionCssClass FilterBorders { get; set; } = FilterSelectionCssClass.Hidden;

    private async Task OnSetFilterAsync(Customer customer)
    {
        FilterKey = customer.Acronym;
        await OnFilterChangedAsync();
    }

    private async Task OnSetFilterAsync(Project project)
    {
        FilterKey = project.ProjectCode;
        await OnFilterChangedAsync();
    }

    private async Task OnUnfundedFilterAsync()
    {
        FilterKey = "?";
        await OnFilterChangedAsync();
    }

    private async Task OnClearFilterAsync()
    {
        FilterKey = "all";
        await OnFilterChangedAsync();
    }

    private async Task OnFilterChangedAsync()
    {
        ResetFilterOnUrl();
        await StoreFilterAsync();
        await OnFilterChanged.InvokeAsync();
    }

    private void ResetFilterOnUrl()
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

    private async Task OnToggleFilterAsync()
    {
        if (CurrentCustomer != null || LastCustomer == null)
        {
            await OnClearFilterAsync();
        }
        else
        {
            await OnSetFilterAsync(LastCustomer);
        }
    }
}
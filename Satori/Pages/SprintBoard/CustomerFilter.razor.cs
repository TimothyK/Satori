using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels;
using Satori.Kimai.ViewModels;

namespace Satori.Pages.SprintBoard;

public partial class CustomerFilter
{
    private Customer[] _customers = [];

    [Parameter]
    public required IReadOnlyCollection<Project> Projects { get; set; }

    [Parameter]
    public EventCallback OnFilterChanged { get; set; }

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
        await OnFilterChanged.InvokeAsync();
    }

    private async Task OnSetFilterAsync(Project project)
    {
        FilterKey = project.ProjectCode;
        await OnFilterChanged.InvokeAsync();
    }

    private async Task OnUnfundedFilterAsync()
    {
        FilterKey = "?";
        await OnFilterChanged.InvokeAsync();
    }

    private async Task OnClearFilterAsync()
    {
        FilterKey = "all";
        await OnFilterChanged.InvokeAsync();
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
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Kimai.ViewModels;

namespace Satori.Pages.SprintBoard;

public partial class SelectProjectDialog : ComponentBase
{
    private WorkItem? WorkItem { get; set; }
    private bool IsOpen { get; set; }
    [Parameter] public EventCallback<(Project?, Activity?)> Save { get; set; }
    [Parameter] public bool RequireActivity { get; set; }

    public Customers? Customers { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && KimaiServer.Enabled)
        {
            Customers = await KimaiServer.GetCustomersAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public void ShowDialog(WorkItem workItem)
    {
        WorkItem = workItem;
        SetValueFromCurrentWorkItem();
        IsOpen = true;
    }

    private void SetValueFromCurrentWorkItem()
    {
        var project = WorkItem?.KimaiProject ?? WorkItem?.Parent?.KimaiProject;
        _selectedCustomer = project?.Customer;
        _selectedProject = project;
        _selectedActivity = WorkItem?.KimaiActivity ?? WorkItem?.Parent?.KimaiActivity;
    }

    private async Task OkAsync()
    {
        IsOpen = false;
        await Save.InvokeAsync((_selectedProject, _selectedActivity));
    }

    private void Cancel()
    {
        IsOpen = false;
    }

    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

    #region Customer

    private Customer? _selectedCustomer;

    private void OnCustomerChanged(Customer customer)
    {
        _selectedCustomer = customer;
        _selectedProject = null;
    }

    private Task<IEnumerable<Customer?>> SearchCustomerAsync(string value, CancellationToken token)
    {
        return Task.FromResult(SearchCustomer(value));
    }

    private IEnumerable<Customer?> SearchCustomer(string value)
    {
        if (Customers == null)
        {
            return [];
        }

        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value))
        {
            return Customers;
        }

        return Customers
            .Where(customer => customer.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
    }

    #endregion Customer


    #region Project

    private Project? _selectedProject;

    private void OnProjectChanged(Project? project)
    {
        _selectedCustomer = project?.Customer ?? _selectedCustomer;
        _selectedProject = project;
        if (project == null)
        {
            _selectedActivity = null;
        }
    }

    private Task<IEnumerable<Project?>> SearchProjectAsync(string value, CancellationToken token)
    {
        return Task.FromResult(SearchProject(value));
    }

    private IEnumerable<Project?> SearchProject(string value)
    {
        var projects = _selectedCustomer?.Projects 
                       ?? Customers?.SelectMany(customer => customer.Projects)
                       ?? [];

        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value))
        {
            return projects;
        }

        return projects
            .Where(project => project.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(project => project.Name);
    }

    #endregion Project

    #region Activity

    private Activity? _selectedActivity;

    private void OnActivityChanged(Activity? activity)
    {
        _selectedProject = activity?.Project ?? _selectedProject;
        _selectedActivity = activity;
    }

    private Task<IEnumerable<Activity?>> SearchActivityAsync(string value, CancellationToken token)
    {
        return Task.FromResult(SearchActivity(value));
    }

    private IEnumerable<Activity?> SearchActivity(string value)
    {
        if (_selectedProject == null)
        {
            return [];
        }

        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value))
        {
            return _selectedProject.Activities;
        }

        return _selectedProject.Activities
            .Where(activity => activity.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(activity => activity.Name);
    }
    

    #endregion Activity
}
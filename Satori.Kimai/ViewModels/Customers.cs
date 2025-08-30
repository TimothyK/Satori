using Satori.Kimai.Utilities;
using System.Collections;
using System.Collections.ObjectModel;
using CustomerModel = Satori.Kimai.Models.Customer;
using ProjectModel = Satori.Kimai.Models.ProjectMaster;
using ActivityModel = Satori.Kimai.Models.ActivityMaster;

namespace Satori.Kimai.ViewModels;

/// <summary>
/// A collection of <see cref="Customer"/> objects with fast lookup support for their projects.
/// </summary>
public class Customers(IEnumerable<CustomerModel> customers) : IEnumerable<Customer>
{
    private readonly ReadOnlyCollection<Customer> _customers = customers.Select(Mappers.ToViewModel).ToList().AsReadOnly();
    private readonly Dictionary<string, Project> _projects = [];

    public IEnumerator<Customer> GetEnumerator()
    {
        return _customers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(ProjectModel model)
    {
        var customer = _customers.FirstOrDefault(cust => cust.Id == model.Customer);
        if (customer == null)
        {
            return;
        }

        var viewModel = Mappers.ToViewModel(model, customer);
        customer.AddProject(viewModel);

        _projects.Add(viewModel.ProjectCode, viewModel);
    }

    public void Add(ActivityModel model)
    {
        if (model.Project == null)  // global activity, not supported for linking to a AzDO Task.
        {
            return;
        }

        var project = _projects.Values.FirstOrDefault(p => p.Id == model.Project);
        if (project == null)
        {
            return;
        }
        var viewModel = Mappers.ToViewModel(model, project);
        project.AddActivity(viewModel);
    }

    public Project? FindProject(string? projectCode)
    {
        if (string.IsNullOrWhiteSpace(projectCode))
            return null;

        projectCode = ProjectCodeParser.GetProjectCode(projectCode);
        return _projects.GetValueOrDefault(projectCode);
    }
}

internal static class Mappers 
{
    public static Customer ToViewModel(CustomerModel dto)
    {
        return new Customer
        {
            Id = dto.Id,
            Name = dto.Name,
            Logo = CustomerParser.GetCustomerLogo(dto.Comment) ?? Customer.DefaultLogo,
            Acronym = CustomerParser.GetAcronym(dto.Name) ?? dto.Name
        };
    }

    public static Project ToViewModel(ProjectModel project, Customer customer)
    {
        return new Project
        {
            Id = project.Id,
            Name = project.Name,
            ProjectCode = ProjectCodeParser.GetProjectCode(project.Name),
            Customer = customer
        };
    }

    public static Activity ToViewModel(ActivityModel activity, Project project)
    {
        return new Activity
        {
            Id = activity.Id,
            Name = activity.Name,
            ActivityCode = ProjectCodeParser.GetActivityCode(activity.Name),
            Project = project
        };
    }
}
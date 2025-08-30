using System.Collections.ObjectModel;
using Flurl;

namespace Satori.Kimai.ViewModels;

public class Customer
{
    internal Customer()
    {
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public override string ToString() => Name;

    public required string Acronym { get; set; }

    public static readonly Uri DefaultLogo = new Url("/images/logo-design.png").ToUri();

    public required Uri Logo { get; set; }

    private readonly List<Project> _projects = [];
    public ReadOnlyCollection<Project> Projects => _projects.AsReadOnly();

    internal void AddProject(Project project)
    {
        _projects.Add(project);
    }
}
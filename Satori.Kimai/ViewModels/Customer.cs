using System.Collections.ObjectModel;

namespace Satori.Kimai.ViewModels;

public class Customer
{
    internal Customer()
    {
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public override string ToString() => Name;

    public Uri? Logo { get; set; }

    private readonly List<Project> _projects = [];
    public ReadOnlyCollection<Project> Projects => _projects.AsReadOnly();

    internal void AddProject(Project project)
    {
        _projects.Add(project);
    }
}
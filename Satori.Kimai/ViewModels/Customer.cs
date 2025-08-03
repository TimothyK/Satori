namespace Satori.Kimai.ViewModels;

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public Uri? Logo { get; set; }

    public List<Project> Projects { get; set; } = [];
}
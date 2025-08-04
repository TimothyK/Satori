namespace Satori.Kimai.ViewModels;

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }

    /// <summary>
    /// How this activity is identified in external systems (e.g. Azure DevOps)
    /// </summary>
    public required string ProjectCode { get; set; }

    public required Customer Customer { get; set; }

    public List<Activity> Activities { get; set; } = [];
}
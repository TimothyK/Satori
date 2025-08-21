namespace Satori.Kimai.ViewModels;

public class Activity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public override string ToString() => Name;


    /// <summary>
    /// How this activity is identified in external systems (e.g. Azure DevOps)
    /// </summary>
    public required string ActivityCode { get; set; }

    public required Project Project { get; set; }
}
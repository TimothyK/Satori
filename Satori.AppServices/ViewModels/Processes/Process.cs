namespace Satori.AppServices.ViewModels.Processes;

public class Process
{
    public Guid Id { get; set; }
    public required string Name { get; set; }

    public Dictionary<string, WorkItemType> WorkItemTypes { get; set; } = [];
    public List<Backlog> Backlogs { get; set; } = [];
    public List<string> Projects { get; set; } = [];
}
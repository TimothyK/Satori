using System.Drawing;

namespace Satori.AppServices.ViewModels.Processes;

public class WorkItemType
{
    public Guid ProcessId { get; set; }

    public required string Name { get; set; }
    internal string ReferenceName { get; set; }
    public Color Color { get; set; }
    public bool IsActive { get; set; }

    public Backlog? Backlog { get; set; }
    public Dictionary<string, State> States { get; set; } = [];
}
namespace Satori.AppServices.ViewModels.Processes;

public class State
{
    public required WorkItemType WorkItemType { get; set; }

    public Guid Id { get; set; }
    public required string Name { get; set; }
    public StateCategory Category { get; set; }
    public int Order { get; set; }
}
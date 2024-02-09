namespace Satori.AppServices.ViewModels.WorkItems;

public class WorkItem
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public Person? AssignedTo { get; set; }
    public Person CreatedBy { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public string IterationPath { get; set; }
    public WorkItemType Type { get; set; }
    public string State { get; set; }
}
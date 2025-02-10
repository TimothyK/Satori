namespace Satori.AppServices.ViewModels.ExportPayloads;

public class DailyActivity
{
    public DateOnly Date { get; internal init; }

    public int ActivityId { get; init; }
    public required string ActivityName { get; init; }
    public string? ActivityDescription { get; init; }
    public int ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public int CustomerId { get; init; }
    public required string CustomerName { get; init; }


    public TimeSpan TotalTime { get; internal init; }

    public string? Accomplishments { get; init; }
    public string? Impediments { get; init; }
    public string? Learnings { get; init; }
    public string? OtherComments { get; init; }
    public string? Tasks { get; init; }
    public required IReadOnlyCollection<WorkItem> WorkItems { get; set; }
}

public class WorkItem 
{
    public int Id { get; set; }
    public required string Title { get; set; }

    /// <summary>
    /// Type of the work item.  
    /// </summary>
    /// <remarks>
    /// <para>
    /// The values here will be the same as the Azure DevOps API
    /// </para>
    /// </remarks>
    public required string Type { get; set; }
}
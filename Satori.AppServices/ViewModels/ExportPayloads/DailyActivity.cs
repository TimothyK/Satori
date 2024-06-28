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
}
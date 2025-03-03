namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ProjectSummary : ISummary
{
    /// <summary>
    /// Kimai internal project id
    /// </summary>
    public int ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public bool IsActive { get; init; }

    public required DaySummary ParentDay { get; init; }

    public int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerAcronym { get; init; }
    public bool CustomerIsActive { get; init; }
    public Uri? CustomerUrl { get; init; }

    public required ActivitySummary[] Activities { get; set; }

    public IEnumerable<TimeEntry> TimeEntries => Activities.SelectMany(a => a.TimeEntries);

    public TimeSpan TotalTime { get; set; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }


    /// <summary>
    /// For the UI to control if the section is collapsed or expanded
    /// </summary>
    public bool IsCollapsed { get; set; }

    public override string ToString() => $"Project {ProjectName} for {ParentDay.Date:yyyy-MM-dd}";
}
using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.ViewModels.DailyStandUps;

public class TimeEntry : ISummary
{
    public int Id { get; init; }

    public required ActivitySummary ParentActivitySummary { get; init; }
    public TaskSummary? ParentTaskSummary { get; set; }

    public DateTimeOffset Begin { get; init; }
    public DateTimeOffset? End { get; set; }
    
    IEnumerable<TimeEntry> ISummary.TimeEntries => this.Yield();
    bool ISummary.AllExported => Exported;

    public TimeSpan TotalTime { get; set; }
    public bool Exported { get; internal set; }
    public bool CanExport { get; internal set; }
    public bool IsRunning { get; internal set; }
    public bool IsOverlapping { get; internal set; }

    /// <summary>
    /// This is read from the comment of the underlying time entry record.
    /// Ideally this will be a Task with a parent PBI/Bug work item, but it might point directly to a PBI/Bug.
    /// </summary>
    public WorkItem? Task { get; set; }
    /// <summary>
    /// Indicates if the Task is missing the Original Estimate field.
    /// This will always be false until a Task is assigned.
    /// </summary>
    public bool NeedsEstimate { get; set; }
    /// <summary>
    /// This is the Remaining Work on the Task (exported) minus all unexported time from any time entry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The "all" unexported time might be fudged to just be the unexported time in the current Daily Stand-Ups reporting period.
    /// There shouldn't be too much unexported time in the system.  So this should be fine.
    /// </para>
    /// </remarks>
    public TimeSpan? TimeRemaining { get; set; }

    public string? Accomplishments { get; set; }
    public string? Impediments { get; set; }
    public string? Learnings { get; set; }
    public string? OtherComments { get; set; }

}
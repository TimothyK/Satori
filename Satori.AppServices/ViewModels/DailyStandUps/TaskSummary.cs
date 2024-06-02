using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.ViewModels.DailyStandUps;

public class TaskSummary
{
    public WorkItem? Task { get; set; }

    public TimeSpan TotalTime { get; internal init; }

    public bool NeedsEstimate { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
}
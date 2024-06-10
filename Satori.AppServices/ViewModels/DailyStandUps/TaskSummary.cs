using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.ViewModels.DailyStandUps;

public class TaskSummary
{
    public WorkItem? Task { get; init; }

    public TimeSpan TotalTime { get; internal init; }

    public bool NeedsEstimate { get; init; }
    public TimeSpan? TimeRemaining { get; init; }
}
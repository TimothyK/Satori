﻿namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ActivitySummary
{
    public int ActivityId { get; init; }
    public required string ActivityName { get; init; }
    public string? Comment { get; init; }

    public required TimeEntry[] TimeEntries { get; init; }

    public TimeSpan TotalTime { get; internal init; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal init; }
    public bool CanExport { get; internal init; }
}
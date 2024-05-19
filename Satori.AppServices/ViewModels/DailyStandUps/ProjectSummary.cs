﻿namespace Satori.AppServices.ViewModels.DailyStandUps;

public class ProjectSummary
{
    public int ProjectId { get; init; }
    public required string ProjectName { get; init; }
    public int CustomerId { get; init; }
    public required string CustomerName { get; init; }
    
    public required ActivitySummary[] Activities { get; init; }
    
    public TimeSpan TotalTime { get; internal init; }
    public required Uri Url { get; init; }
    public bool AllExported { get; internal init; }
    public bool CanExport { get; internal init; }
}
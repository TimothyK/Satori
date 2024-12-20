﻿using Satori.Converters;
using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

/// <summary>
/// A work item (PBI, Bug, Feature, Epic, Task, etc)
/// </summary>
/// <remarks>
/// <para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/list?view=azure-devops-rest-6.0&tabs=HTTP#workitem
/// </para>
/// </remarks>
public class WorkItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("rev")]
    public int Rev { get; set; }
    [JsonPropertyName("fields")]
    public required WorkItemFields Fields { get; set; }
    [JsonPropertyName("relations")] 
    public List<WorkItemRelation> Relations { get; set; } = [];
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

public class WorkItemFields
{
    [JsonPropertyName("System.AreaPath")]
    public string? AreaPath { get; set; }
    [JsonPropertyName("System.TeamProject")]
    public required string ProjectName { get; set; }
    [JsonPropertyName("System.IterationPath")]
    public string? IterationPath { get; set; }
    [JsonPropertyName("System.WorkItemType")]
    public required string WorkItemType { get; set; }
    [JsonPropertyName("System.State")]
    public required string State { get; set; }
    [JsonPropertyName("System.Reason")]
    public string? SystemReason { get; set; }
    [JsonPropertyName("System.AssignedTo")]
    public User? AssignedTo { get; set; }
    [JsonPropertyName("System.CreatedDate")]
    public DateTimeOffset SystemCreatedDate { get; set; }
    [JsonPropertyName("System.CreatedBy")]
    public required User CreatedBy { get; set; }
    [JsonPropertyName("System.ChangedDate")]
    public DateTimeOffset SystemChangedDate { get; set; }
    [JsonPropertyName("System.ChangedBy")]
    public required User SystemChangedBy { get; set; }
    [JsonPropertyName("System.CommentCount")]
    public int CommentCount { get; set; }

    [JsonPropertyName("System.Title")]
    public required string Title { get; set; }

    [JsonPropertyName("Microsoft.VSTS.Scheduling.OriginalEstimate")]
    public double? OriginalEstimate { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Scheduling.CompletedWork")]
    public double? CompletedWork { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Scheduling.RemainingWork")]
    public double? RemainingWork { get; set; }

    [JsonPropertyName("Microsoft.VSTS.Common.Priority")]
    public int Priority { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Common.Severity")]
    public string? Severity { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Common.ValueArea")]
    public string? ValueArea { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Common.Triage")]
    public string? Triage { get; set; }
    [JsonPropertyName("System.Tags")]
    public string? Tags { get; set; }
    [JsonPropertyName("Microsoft.VSTS.CMMI.Blocked")]
    [JsonConverter(typeof(YesNoConverter))]
    public bool Blocked { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Common.BacklogPriority")]
    public double BacklogPriority { get; set; }

    /// <summary>
    /// Available on PBI & Bug
    /// </summary>
    [JsonPropertyName("Custom.ProjectCode")]
    public string? ProjectCode { get; set; }

    /// <summary>
    /// Available on all work item types
    /// </summary>
    [JsonPropertyName("System.Description")]
    public string? Description { get; set; }
    /// <summary>
    /// Bug - System information of the environment that the issue is reproducible in.  Program version numbers, path to DB backup, Shipment/Sample IDs, etc.
    /// </summary>
    [JsonPropertyName("Microsoft.VSTS.TCM.SystemInfo")]
    public string? SystemInfo { get; set; }
    /// <summary>
    /// Bug Reproduction steps
    /// </summary>
    [JsonPropertyName("Microsoft.VSTS.TCM.ReproSteps")]
    public string? ReproSteps { get; set; }
    /// <summary>
    /// Bug Observed Result
    /// </summary>
    [JsonPropertyName("Custom.ObservedBehavior")]
    public string? ObservedBehavior { get; set; }
    /// <summary>
    /// Bug, PBI, Feature, Epic, but not Task.   Expected Result on a Bug.  
    /// </summary>
    [JsonPropertyName("Microsoft.VSTS.Common.AcceptanceCriteria")]
    public string? AcceptanceCriteria { get; set; }
    /// <summary>
    /// PBI - User Story
    /// </summary>
    [JsonPropertyName("Custom.UserStory")]
    public string? UserStory { get; set; }
    /// <summary>
    /// Bug - Impact.
    /// </summary>
    /// <seealso cref="ImpactAssessment"/>
    [JsonPropertyName("Custom.Impact")]
    public string? Impact { get; set; }
    /// <summary>
    /// PBI - Impact
    /// </summary>
    /// <seealso cref="Impact"/>
    [JsonPropertyName("Microsoft.VSTS.CMMI.ImpactAssessmentHtml")]
    public string? ImpactAssessment { get; set; }
    /// <summary>
    /// PBI - Enhancement.  Like <see cref="ProposedFix"/> on a Bug
    /// </summary>
    /// <seealso cref="ProposedFix"/>
    [JsonPropertyName("Custom.Enhancement")]
    public string? Enhancement { get; set; }
    /// <summary>
    /// Bug - Proposed Fix
    /// </summary>
    /// <seealso cref="Enhancement"/>
    [JsonPropertyName("Microsoft.VSTS.CMMI.ProposedFix")]
    public string? ProposedFix { get; set; }

    [JsonPropertyName("System.Parent")]
    public int? Parent { get; set; }

}


/// <summary>
/// All relations to a work item.
/// </summary>
/// <remarks><para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/list?view=azure-devops-rest-6.0&tabs=HTTP#workitemrelation
/// </para></remarks>
public class WorkItemRelation
{
    [JsonPropertyName("attributes")]
    public required Dictionary<string, object> Attributes { get; set; }
    [JsonPropertyName("rel")]
    public required string RelationType { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

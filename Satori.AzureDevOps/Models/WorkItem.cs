using System.Text.Json.Serialization;
using Satori.AzureDevOps.Converters;

namespace Satori.AzureDevOps.Models;

public class WorkItem
{
    public int id { get; set; }
    public int rev { get; set; }
    public Fields fields { get; set; }
    public string url { get; set; }
}

public class Fields
{
    [JsonPropertyName("System.AreaPath")]
    public string AreaPath { get; set; }
    public string SystemTeamProject { get; set; }
    [JsonPropertyName("System.IterationPath")]
    public string IterationPath { get; set; }
    [JsonPropertyName("System.WorkItemType")]
    public string WorkItemType { get; set; }
    [JsonPropertyName("System.State")]
    public string State { get; set; }
    public string SystemReason { get; set; }
    [JsonPropertyName("System.AssignedTo")]
    public User? AssignedTo { get; set; }
    [JsonPropertyName("System.CreatedDate")] 
    public DateTimeOffset SystemCreatedDate { get; set; }
    [JsonPropertyName("System.CreatedBy")]
    public required User CreatedBy { get; set; }
    public DateTime SystemChangedDate { get; set; }
    public User SystemChangedBy { get; set; }
    [JsonPropertyName("System.CommentCount")]
    public int CommentCount { get; set; }
    [JsonPropertyName("System.Title")]
    public string SystemTitle { get; set; }

    [JsonPropertyName("Microsoft.VSTS.Common.Priority")]
    public int Priority { get; set; }
    public string MicrosoftVSTSCommonSeverity { get; set; }
    public string MicrosoftVSTSCommonValueArea { get; set; }
    public string MicrosoftVSTSCommonTriage { get; set; }
    [JsonPropertyName("Microsoft.VSTS.CMMI.Blocked")]
    [JsonConverter(typeof(YesNoConverter))]
    public bool Blocked { get; set; }
    [JsonPropertyName("Microsoft.VSTS.Common.BacklogPriority")]
    public double BacklogPriority { get; set; }

    [JsonPropertyName("Custom.ProjectCode")]
    public string ProjectCode { get; set; }
    public string MicrosoftVSTSTCMSystemInfo { get; set; }
    public string MicrosoftVSTSTCMReproSteps { get; set; }
    public string CustomObservedBehavior { get; set; }
}
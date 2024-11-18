using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Satori.AzureDevOps.Models;


/// <summary>
/// Container for the work items linked to an iteration.
/// </summary>
/// <remarks>
/// <para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/iterations/get-iteration-work-items?view=azure-devops-rest-6.0&tabs=HTTP#iterationworkitems
/// </para>
/// </remarks>
public class IterationWorkItems
{
    [JsonPropertyName("workItemRelations")]
    public required WorkItemLink[] WorkItemRelations { get; set; }
}

/// <summary>
/// Link between parent and child work items.
/// </summary>
/// <remarks>
/// <para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/iterations/get-iteration-work-items?view=azure-devops-rest-6.0&tabs=HTTP#workitemlink
/// </para>
/// </remarks>
public class WorkItemLink
{
    [JsonPropertyName("source")]
    public WorkItemReference? Source { get; set; }
    [JsonPropertyName("target")]
    public required WorkItemReference Target { get; set; }
}

/// <summary>
/// Strongly typed complex type for a work item reference.
/// </summary>
/// <remarks>
/// <para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/iterations/get-iteration-work-items?view=azure-devops-rest-6.0&tabs=HTTP#workitemreference
/// </para>
/// </remarks>
public class WorkItemReference
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
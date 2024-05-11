using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

/// <summary>
/// Payload returned from a Reorder PATCH response
/// </summary>
/// <remarks>
/// <para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/workitemsorder/reorder-backlog-work-items?view=azure-devops-rest-6.0&tabs=HTTP#reorderresult
/// </para>
/// <para>
/// These objects will be wrapped in the <see cref="RootObject{T}.Value"/> property
/// </para>
/// </remarks>
public class ReorderResult
{
    /// <summary>
    /// Work Item ID of the Work Item that was reordered
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// New value for the <see cref="WorkItemFields.BacklogPriority"/>
    /// </summary>
    [JsonPropertyName("order")]
    public double Order { get; set; }
}

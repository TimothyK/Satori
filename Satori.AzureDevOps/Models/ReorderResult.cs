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
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("order")]
    public double Order { get; set; }
}

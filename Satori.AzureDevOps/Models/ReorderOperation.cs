using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

/// <summary>
/// Payload sent with a Reorder PATCH request
/// </summary>
/// <remarks>
/// <para>
/// https://learn.microsoft.com/en-us/rest/api/azure/devops/work/workitemsorder/reorder-backlog-work-items?view=azure-devops-rest-6.0&tabs=HTTP#reorderoperation
/// </para>
/// </remarks>
public class ReorderOperation
{
    [JsonPropertyName("parentId")]
    public int ParentId { get; set; }
    [JsonPropertyName("previousId")]
    public int PreviousId { get; set; }
    [JsonPropertyName("nextId")]
    public int NextId { get; set; }
    [JsonPropertyName("ids")]
    public int[] Ids { get; set; }
}

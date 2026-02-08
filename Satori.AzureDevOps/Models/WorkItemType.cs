using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
/// <summary>
/// ProcessWorkItemType (https://learn.microsoft.com/en-us/rest/api/azure/devops/processes/work-item-types/list?view=azure-devops-rest-7.1&tabs=HTTP#processworkitemtype)
/// </summary>
public class WorkItemType
{
    [JsonPropertyName("referenceName")]
    public required string ReferenceName { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("color")]
    public required string Color { get; set; }
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }
    [JsonPropertyName("states")]
    public required State[] States { get; set; }
}
using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

/// <summary>
/// WorkItemStateResultModel (https://learn.microsoft.com/en-us/rest/api/azure/devops/processes/work-item-types/list?view=azure-devops-rest-7.1&tabs=HTTP#workitemstateresultmodel)
/// </summary>
public class State
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("stateCategory")]
    public required string StateCategory { get; set; }
    [JsonPropertyName("order")]
    public int Order { get; set; }
}
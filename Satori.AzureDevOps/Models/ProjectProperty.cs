using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
/// <summary>
/// ProjectProperty (https://learn.microsoft.com/en-us/rest/api/azure/devops/core/projects/get-project-properties?view=azure-devops-rest-7.1&tabs=HTTP#projectproperty)
/// </summary>
public class ProjectProperty
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("value")]
    public required object Value { get; set; }
}
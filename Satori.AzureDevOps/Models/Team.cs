using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class Team
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("projectName")]
    public required string ProjectName { get; set; }
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}
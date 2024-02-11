using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class Project
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
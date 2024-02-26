using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable UnusedAutoPropertyAccessor.Global
public class User
{
    [JsonPropertyName("_links")]
    public Links? Links { get; set; }
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("imageUrl")]
    public required string ImageUrl { get; set; }
    [JsonPropertyName("uniqueName")]
    public required string UniqueName { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}
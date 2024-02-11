using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class Avatar
{
    [JsonPropertyName("href")]
    public required string Href { get; set; }
}
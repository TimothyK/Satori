using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class Links
{
    [JsonPropertyName("avatar")]
    public Avatar? Avatar { get; set; }
}
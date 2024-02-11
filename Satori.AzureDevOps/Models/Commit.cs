using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class Commit
{
    [JsonPropertyName("commitId")]
    public required string CommitId { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}
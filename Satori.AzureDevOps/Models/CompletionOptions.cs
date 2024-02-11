using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class CompletionOptions
{
    [JsonPropertyName("mergeStrategy")]
    public string? MergeStrategy { get; set; }
    [JsonPropertyName("deleteSourceBranch")]
    public bool DeleteSourceBranch { get; set; }
    [JsonPropertyName("mergeCommitMessage")]
    public string? MergeCommitMessage { get; set; }
    [JsonPropertyName("triggeredByAutoComplete")]
    public bool TriggeredByAutoComplete { get; set; }
}
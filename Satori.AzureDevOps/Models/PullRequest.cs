using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class PullRequest
{
    [JsonPropertyName("createdBy")]
    public required User CreatedBy { get; set; }
    [JsonPropertyName("creationDate")]
    public DateTimeOffset CreationDate { get; set; }
    /// <summary>
    /// This should be treated as the draft Release Note
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("isDraft")]
    public bool IsDraft { get; set; }
    [JsonPropertyName("lastMergeCommit")]
    public required Commit LastMergeCommit { get; set; }
    [JsonPropertyName("lastMergeSourceCommit")]
    public required Commit LastMergeSourceCommit { get; set; }
    [JsonPropertyName("lastMergeTargetCommit")]
    public required Commit LastMergeTargetCommit { get; set; }
    [JsonPropertyName("mergeId")]
    public Guid MergeId { get; set; }
    [JsonPropertyName("mergeStatus")]
    public required string MergeStatus { get; set; }
    [JsonPropertyName("pullRequestId")]
    public int PullRequestId { get; set; }
    [JsonPropertyName("repository")]
    public required Repository Repository { get; set; }
    [JsonPropertyName("reviewers")]
    public required Reviewer[] Reviewers { get; set; }
    [JsonPropertyName("sourceRefName")]
    public required string SourceRefName { get; set; }
    [JsonPropertyName("status")]
    public required string Status { get; set; }
    [JsonPropertyName("supportsIterations")]
    public bool SupportsIterations { get; set; }
    [JsonPropertyName("targetRefName")]
    public required string TargetRefName { get; set; }
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
    [JsonPropertyName("completionOptions")]
    public CompletionOptions? CompletionOptions { get; set; }
    [JsonPropertyName("autoCompleteSetBy")]
    public User? AutoCompleteSetBy { get; set; }
    [JsonPropertyName("labels")]
    public Label[]? Labels { get; set; }
}
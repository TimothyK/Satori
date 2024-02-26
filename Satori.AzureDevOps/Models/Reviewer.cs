using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class Reviewer : User
{
    [JsonPropertyName("hasDeclined")]
    public bool HasDeclined { get; set; }
    [JsonPropertyName("isFlagged")]
    public bool IsFlagged { get; set; }
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }
    [JsonPropertyName("reviewerUrl")]
    public required string ReviewerUrl { get; set; }
    [JsonPropertyName("vote")]
    public int Vote { get; set; }
}
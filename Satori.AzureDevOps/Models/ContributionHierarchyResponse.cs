using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

/// <summary>
/// Response from POST to https://devops.test/Org/_apis/Contribution/HierarchyQuery/project/projectId?api-version=5.0-preview.1
/// </summary>
/// <seealso cref="ContributionHierarchyQuery"/>
public class ContributionHierarchyResponse
{
    [JsonPropertyName("dataProviders")]
    public required DataProviders DataProviders { get; set; }
}

public class DataProviders
{
    [JsonPropertyName(DataProviderIds.CommitsDataProvider)]
    public CommitsDataProvider? CommitsDataProvider { get; set; }
}

public class CommitsDataProvider
{
    [JsonPropertyName("tags")]
    public Dictionary<string, Tag[]>? Tags { get; set; }
}

public class Tag
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("comment")]
    public required string Comment { get; set; }
    [JsonPropertyName("tagger")]
    public required Tagger Tagger { get; set; }
    [JsonPropertyName("objectId")]
    public required string ObjectId { get; set; }
    [JsonPropertyName("resolvedCommitId")]
    public required string ResolvedCommitId { get; set; }
}

public class Tagger
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("date")]
    public required DateTimeOffset Date { get; set; }
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}
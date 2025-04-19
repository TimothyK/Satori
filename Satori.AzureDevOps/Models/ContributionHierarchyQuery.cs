using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;
public static class DataProviderIds
{
    public const string CommitsDataProvider = "ms.vss-code-web.commits-data-provider";
}

/// <summary>
/// Payload for POST to https://devops.test/Org/_apis/Contribution/HierarchyQuery/project/projectId?api-version=5.0-preview.1
/// </summary>
/// <seealso cref="ContributionHierarchyResponse"/>
public class ContributionHierarchyQuery
{
    /// <summary>
    /// Values should be one of <see cref="DataProviderIds"/>
    /// </summary>
    [JsonPropertyName("contributionIds")]
    public required string[] ContributionIds { get; set; }
    [JsonPropertyName("dataProviderContext")]
    public required DataProviderContext DataProviderContext { get; set; }
}

public class DataProviderContext
{
    [JsonPropertyName("properties")]
    public required DataProviderContextProperties Properties { get; set; }
}

public class DataProviderContextProperties
{
    [JsonPropertyName("searchCriteria")]
    public required SearchCriteria SearchCriteria { get; set; }
    [JsonPropertyName("repositoryId")]
    public required Guid RepositoryId { get; set; }
}

public class SearchCriteria
{
    [JsonPropertyName("gitArtifactsQueryArguments")]
    public required GitArtifactsQueryArguments GitArtifactsQueryArguments { get; set; }
}

public class GitArtifactsQueryArguments
{
    [JsonPropertyName("fetchBuildStatuses")]
    public bool FetchBuildStatuses { get; set; }
    [JsonPropertyName("fetchPullRequests")]
    public bool FetchPullRequests { get; set; }
    [JsonPropertyName("fetchTags")]
    public bool FetchTags { get; set; }
    [JsonPropertyName("commitIds")]
    public required string[] CommitIds { get; set; }
}
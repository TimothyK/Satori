using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

/// <summary>
/// BacklogConfiguration (https://learn.microsoft.com/en-us/rest/api/azure/devops/work/backlogconfiguration/get?view=azure-devops-rest-7.1&tabs=HTTP#backlogconfiguration)
/// </summary>
public class BacklogConfiguration
{
    [JsonPropertyName("taskBacklog")]
    public required BacklogLevelConfiguration TaskBacklog { get; set; }
    [JsonPropertyName("requirementBacklog")]
    public required BacklogLevelConfiguration RequirementBacklog { get; set; }
    [JsonPropertyName("portfolioBacklogs")]
    public required BacklogLevelConfiguration[] PortfolioBacklogs { get; set; }
}

/// <summary>
/// BacklogLevelConfiguration https://learn.microsoft.com/en-us/rest/api/azure/devops/work/backlogconfiguration/get?view=azure-devops-rest-7.1&tabs=HTTP#backloglevelconfiguration
/// </summary>
public class BacklogLevelConfiguration
{   
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("rank")]
    public int Rank { get; set; }
    [JsonPropertyName("workItemTypes")]
    public required WorkItemTypeReference[] WorkItemTypes { get; set; }
    [JsonPropertyName("defaultWorkItemType")]
    public required WorkItemTypeReference DefaultWorkItemType { get; set; }
    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

/// <summary>
/// WorkItemTypeReference (https://learn.microsoft.com/en-us/rest/api/azure/devops/work/backlogconfiguration/get?view=azure-devops-rest-7.1&tabs=HTTP#workitemtypereference)
/// </summary>
public class WorkItemTypeReference
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

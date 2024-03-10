using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Satori.AzureDevOps.Models;


public class WorkItemRelationRoot
{
    [JsonPropertyName("workItemRelations")]
    public required WorkItemRelation[] WorkItemRelations { get; set; }
}

public class WorkItemRelation
{
    [JsonPropertyName("source")]
    public WorkItemId? Source { get; set; }
    [JsonPropertyName("target")]
    public required WorkItemId Target { get; set; }
}

public class WorkItemId
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}
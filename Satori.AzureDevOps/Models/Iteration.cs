// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

/// <summary>
/// TeamSettingsIteration (https://learn.microsoft.com/en-us/rest/api/azure/devops/work/iterations/list?view=azure-devops-rest-7.1&tabs=HTTP#teamsettingsiteration)
/// </summary>
public class Iteration
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("path")]
    public required string Path { get; set; }
    [JsonPropertyName("attributes")]
    public required IterationAttributes Attributes { get; set; }
}

public class IterationAttributes
{
    [JsonPropertyName("startDate")]
    public DateTimeOffset? StartDate { get; set; }
    [JsonPropertyName("finishDate")]
    public DateTimeOffset? FinishDate { get; set; }
    [JsonPropertyName("timeFrame")]
    public required string TimeFrame { get; set; }
}
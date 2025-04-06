using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

public class WorkItemPatchItem
{
    [JsonPropertyName("op")]
    public Operation Operation { get; init; }
    [JsonPropertyName("path")]
    public required string Path { get; init; }
    [JsonPropertyName("value")]
    public required object Value { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<Operation>))]
public enum Operation
{
    Add,
    //Remove,
    Replace,
    //Move,
    //Copy,
    Test
}
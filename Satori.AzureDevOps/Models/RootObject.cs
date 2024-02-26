using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

public class RootObject<T>
{
    [JsonPropertyName("count")]
    public int Count { get; init; }
    [JsonPropertyName("value")]
    public required T[] Value { get; init; }
}
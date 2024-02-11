using Satori.AzureDevOps.Converters;
using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

// ReSharper disable once ClassNeverInstantiated.Global
public class IdMap
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(IntAsStringConverter))]
    public int Id { get; set; }
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}
using System.Text.Json.Serialization;
using Satori.Converters;

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
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Satori.Kimai.Models;

public class Customer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}
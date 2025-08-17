using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Satori.Kimai.Models;

public class Activity
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("project")]
    public Project? Project { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}


public class ActivityMaster
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("project")]
    public int? Project { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
}
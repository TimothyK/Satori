using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Satori.Kimai.Models;

public class User
{
    [JsonPropertyName("preferences")] 
    public Preference[]? Preferences { get; set; }

    [JsonPropertyName("timezone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }

    [JsonPropertyName("avatar")]
    public Uri? Avatar { get; set; }

    [JsonPropertyName("username")]
    public required string UserName { get; set; }

    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

public class Preference
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
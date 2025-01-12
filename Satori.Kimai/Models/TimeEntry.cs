using System.Text.Json.Serialization;
using Satori.Converters;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Satori.Kimai.Models;

public class TimeEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("begin")]
    [JsonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset Begin { get; set; }

    [JsonPropertyName("end")]
    [JsonConverter(typeof(NullableDateTimeOffsetConverter))]
    public DateTimeOffset? End { get; set; }

    [JsonPropertyName("user")]
    public required User User { get; set; }

    [JsonPropertyName("activity")]
    public required Activity Activity { get; set; }

    [JsonPropertyName("project")]
    public required Project Project { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("exported")]
    public bool Exported { get; set; }

    public bool IsOverlapping { get; set; }
}


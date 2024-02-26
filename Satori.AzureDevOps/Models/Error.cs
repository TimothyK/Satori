using System.Text.Json.Serialization;

namespace Satori.AzureDevOps.Models;

public class Error
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    [JsonPropertyName("message")]
    public required string Message { get; init; }
    [JsonPropertyName("typeName")]
    public string? TypeName { get; init; }
    [JsonPropertyName("typeKey")]
    public required string TypeKey { get; init; }
    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; init; }
    [JsonPropertyName("eventId")]
    public int EventId { get; init; }
}
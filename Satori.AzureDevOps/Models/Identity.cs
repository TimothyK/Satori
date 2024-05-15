using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Satori.AzureDevOps.Models;

public class Identity
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("providerDisplayName")]
    public required string ProviderDisplayName { get; set; }
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
    [JsonPropertyName("properties")]
    public required IdentityProperties Properties { get; set; }
}

public class IdentityProperties
{
    public IdentityPropertyValue<string>? Description { get; set; }
    public IdentityPropertyValue<string>? Domain { get; set; }
    public IdentityPropertyValue<string>? Account { get; set; }
    public IdentityPropertyValue<string>? Mail { get; set; }
    public IdentityPropertyValue<DateTimeOffset>? ComplianceValidated { get; set; }
}

public class IdentityPropertyValue<T>
{
    [JsonPropertyName("$value")]
    public required T Value { get; set; }
}


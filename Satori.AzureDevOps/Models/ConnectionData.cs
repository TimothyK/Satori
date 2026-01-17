using System.Text.Json.Serialization;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Satori.AzureDevOps.Models;

public class ConnectionData
{
    [JsonPropertyName("authenticatedUser")]
    public required ConnectionUser AuthenticatedUser { get; set; }

    /// <summary>
    /// Values will be "hosted" or "onPremises"
    /// </summary>
    [JsonPropertyName("deploymentType")]
    public required string DeploymentType { get; set; }
}

public class ConnectionUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
}


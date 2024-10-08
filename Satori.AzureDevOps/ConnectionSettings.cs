﻿using Satori.Converters;
using System.Text.Json.Serialization;

namespace Satori.AzureDevOps;

public class ConnectionSettings
{
    public bool Enabled { get; init; } = true;
    public required Uri Url { get; init; }
    [JsonConverter(typeof(EncryptedStringConverter))]
    public required string PersonalAccessToken { get; init; }

    public static readonly ConnectionSettings Default = new()
    {
        Enabled = false,
        Url = new Uri("https://devops.test/Org"),
        PersonalAccessToken = string.Empty,
    };
}
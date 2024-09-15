namespace Satori.MessageQueues;

public class ConnectionSettings
{
    public static readonly ConnectionSettings Default = new()
    {
        Enabled = false,
        Subdomain = string.Empty,
        QueueName = "UserName",
        KeyName = "Send",
        Key = string.Empty,
    };

    public bool Enabled { get; init; } = true;
    public required string Subdomain { get; init; }
    public required string QueueName { get; init; }
    public required string KeyName { get; init; }
    public required string Key { get; init; }
}
using RabbitMQ.Client;

namespace Satori.MessageQueues;

public class ConnectionSettings
{
    /// <summary>
    /// Default settings for Rabbit MQ installed on the same computer
    /// </summary>
    public static readonly ConnectionSettings Default = new()
    {
        Enabled = false,
        HostName = "localhost",
        Port = 5672,
        PortalPort = 15672,
        Path = "/",
        UserName = "guest",
        Password = "guest"
    };

    public bool Enabled { get; init; } = true;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; }
    public int PortalPort { get; set; } = 15672;

    public string? Path { get; set; }

    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";


    internal (IConnection connection, IModel channel) Open()
    {
        var factory = new ConnectionFactory()
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            VirtualHost = Path ?? "/",
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        return (connection, channel);
    }
}
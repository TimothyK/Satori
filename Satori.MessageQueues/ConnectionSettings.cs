using RabbitMQ.Client;
using System.Threading.Channels;

namespace Satori.MessageQueues;

public class ConnectionSettings
{
    /// <summary>
    /// Default settings for Rabbit MQ installed on the same computer
    /// </summary>
    public static ConnectionSettings Default { get; } = new ConnectionSettings
    {
        Port = 5672,
        Path = "/",
        UserName = "guest",
        Password = "guest"
    };

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; }

    public string? Path { get; set; }

    public string UserName { get; set; } = "guest";
    public string Password { internal get; set; } = "guest";


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
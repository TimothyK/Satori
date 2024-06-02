using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Satori.MessageQueues;

public sealed class Publisher<T> : IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;

    private string? ExchangeName { get; set; }

    public void Open(ConnectionSettings settings, string name)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(name);
        ExchangeName = name;

        (_connection, _channel) = settings.Open();

        CreateExchangeAndQueue(name);
    }

    private void CreateExchangeAndQueue(string name)
    {
        _channel.ExchangeDeclare(name, "direct", durable: true);
        _channel.QueueDeclare(name, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(name, name, routingKey: null);
    }

    public void Close()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        _channel = null;
        _connection = null;
    }

    #region Dispose

    private bool _disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            // Dispose managed resources.
            Close();
        }

        // Dispose unmanaged resources.

        _disposed = true;
    }

    ~Publisher()
    {
        Dispose(false);
    }

    #endregion Dispose

    public void Send(T message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (_channel == null)
        {
            throw new InvalidOperationException("The channel is not open");
        }
        
        var routingKey = typeof(T).FullName;

        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        _channel.BasicPublish(ExchangeName, routingKey, properties, payload);
    }
}
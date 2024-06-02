using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Satori.MessageQueues;

public class Subscription<T> : IDisposable
{
    public string QueueName { get; }
    private readonly ConnectionSettings _settings;
    private readonly Action<T> _onReceive;

    public Subscription(ConnectionSettings settings, string queueName, Action<T> onReceive)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(queueName);
        ArgumentNullException.ThrowIfNull(onReceive);

        _settings = settings;
        QueueName = queueName;
        _onReceive = onReceive;
    }

    private IConnection? _connection;
    private IModel? _channel;

    public void Start()
    {
        GetPublisher().Dispose();  // Ensure the queue exists.  The publisher will create it.

        (_connection, _channel) = _settings.Open();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, e) =>
        {
            var body = e.Body.ToArray();
            var message = JsonSerializer.Deserialize<T>(body) 
                          ?? throw new InvalidOperationException("Null Message on queue");
            _onReceive(message);
        };
        _channel.BasicConsume(QueueName, autoAck: true, consumer);
    }

    public Publisher<T> GetPublisher()
    {
        var publisher = new Publisher<T>();
        publisher.Open(_settings, QueueName);
        return publisher;
    }

    private void Close()
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

    protected virtual void Dispose(bool disposing)
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

    ~Subscription()
    {
        Dispose(false);
    }

    #endregion Dispose

}
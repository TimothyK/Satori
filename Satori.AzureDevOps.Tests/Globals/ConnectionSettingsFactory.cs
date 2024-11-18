namespace Satori.AzureDevOps.Tests.Globals;

internal class ConnectionSettingsFactory
{
    private readonly object _setterLock = new();
    private ConnectionSettingsSetter? _setter;

    private ConnectionSettings DefaultConnectionSettings { get; } = new()
    {
        Url = new Uri("http://devops.test/Org"),
        PersonalAccessToken = "test"
    };

    public IDisposable Set(ConnectionSettings connectionSettings)
    {
        lock (_setterLock)
        {
            if (_setter != null)
            {
                throw new InvalidOperationException("Connection settings already set");
            }

            var setter = new ConnectionSettingsSetter(connectionSettings);
            setter.Disposing += ClearSetter;

            _setter = setter;

            return setter;
        }
    }

    private void ClearSetter(object? sender, EventArgs e)
    {
        lock (_setterLock)
        {
            _setter = null;
        }
    }

    public ConnectionSettings GetConnectionSettings()
    {
        return _setter != null ? _setter.ConnectionSettings 
            : DefaultConnectionSettings;
    }
}

internal class ConnectionSettingsSetter(ConnectionSettings connectionSettings) : IDisposable
{
    public ConnectionSettings ConnectionSettings { get; } = connectionSettings;

    public event EventHandler? Disposing;

    public void Dispose()
    {
        Disposing?.Invoke(this, EventArgs.Empty);
    }
}
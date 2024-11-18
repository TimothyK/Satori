namespace Satori.AzureDevOps.Tests.Globals;

internal class ConnectionSettingsFactory
{
    public ConnectionSettings ConnectionSettings { get; set; } = new()
    {
        Url = new Uri("http://devops.test/Org"),
        PersonalAccessToken = "test"
    };

    public ConnectionSettings GetConnectionSettings()
    {
        return ConnectionSettings;
    }
}
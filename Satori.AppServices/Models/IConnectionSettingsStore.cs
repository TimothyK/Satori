namespace Satori.AppServices.Models;

public interface IConnectionSettingsStore
{
    AzureDevOps.ConnectionSettings GetAzureDevOpsSettings();
    Kimai.ConnectionSettings GetKimaiSettings();
    MessageQueues.ConnectionSettings GetMessageQueueSettings();

    void SetAzureDevOpsSettings(AzureDevOps.ConnectionSettings settings);
    void SetKimaiSettings(Kimai.ConnectionSettings settings);
    void SetMessageQueueSettings(MessageQueues.ConnectionSettings settings);
}
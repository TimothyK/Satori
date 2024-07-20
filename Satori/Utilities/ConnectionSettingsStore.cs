using Satori.AppServices.Models;
using ConnectionSettings = Satori.AzureDevOps.ConnectionSettings;

namespace Satori.Utilities
{
    internal class ConnectionSettingsStore(
        Blazored.LocalStorage.ILocalStorageService localStorage,
        AppServices.Models.ConnectionSettings oldAppSettings
    ) : IConnectionSettingsStore
    {
        public ConnectionSettings GetAzureDevOpsSettings()
        {
            return oldAppSettings.AzureDevOps;
        }

        public Kimai.ConnectionSettings GetKimaiSettings()
        {
            return oldAppSettings.Kimai;
        }

        public MessageQueues.ConnectionSettings GetMessageQueueSettings()
        {
            return oldAppSettings.MessageQueue;
        }

        public void SetAzureDevOpsSettings(ConnectionSettings settings)
        {
            throw new NotImplementedException();
        }

        public void SetKimaiSettings(Kimai.ConnectionSettings settings)
        {
            throw new NotImplementedException();
        }

        public void SetMessageQueueSettings(MessageQueues.ConnectionSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}

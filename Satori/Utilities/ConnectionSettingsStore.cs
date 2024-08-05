using Satori.AppServices.Models;
using ConnectionSettings = Satori.AzureDevOps.ConnectionSettings;

namespace Satori.Utilities
{
    internal class ConnectionSettingsStore(
        Blazored.LocalStorage.ISyncLocalStorageService  localStorage,
        AppServices.Models.ConnectionSettings oldAppSettings
    ) : IConnectionSettingsStore
    {
        private static class LocalStorageKeys
        {
            public const string AzureDevOpsSettings = "AzureDevOpsSettings";
        }

        public ConnectionSettings GetAzureDevOpsSettings()
        {
            return oldAppSettings.AzureDevOps;
            //return localStorage.GetItem<ConnectionSettings>(LocalStorageKeys.AzureDevOpsSettings)
            //    ?? ConnectionSettings.Default;
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
            localStorage.SetItem(LocalStorageKeys.AzureDevOpsSettings, settings);
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

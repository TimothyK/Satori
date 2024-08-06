using Satori.AppServices.Models;
using ConnectionSettings = Satori.AzureDevOps.ConnectionSettings;

namespace Satori.Utilities
{
    internal class ConnectionSettingsStore(
        Blazored.LocalStorage.ILocalStorageService  localStorage,
        AppServices.Models.ConnectionSettings oldAppSettings
    ) : IConnectionSettingsStore
    {
        private static class LocalStorageKeys
        {
            public const string AzureDevOpsSettings = "AzureDevOpsSettings";
            public const string KimaiSettings = "KimaiSettings";
            public const string MessageQueueSettings = "MessageQueueSettings";
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
            localStorage.SetItemAsync(LocalStorageKeys.AzureDevOpsSettings, settings);
        }

        public void SetKimaiSettings(Kimai.ConnectionSettings settings)
        {
            localStorage.SetItemAsync(LocalStorageKeys.KimaiSettings, settings);
        }

        public void SetMessageQueueSettings(MessageQueues.ConnectionSettings settings)
        {
            localStorage.SetItemAsync(LocalStorageKeys.MessageQueueSettings, settings);
        }
    }
}

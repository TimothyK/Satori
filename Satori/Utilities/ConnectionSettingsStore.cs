using Satori.AppServices.Models;

namespace Satori.Utilities
{
    internal class ConnectionSettingsStore(
        Blazored.LocalStorage.ISyncLocalStorageService localStorage
    ) : IConnectionSettingsStore
    {
        private static class LocalStorageKeys
        {
            public const string AzureDevOpsSettings = "AzureDevOpsSettings";
            public const string KimaiSettings = "KimaiSettings";
            public const string MessageQueueSettings = "MessageQueueSettings";
        }

        public AzureDevOps.ConnectionSettings GetAzureDevOpsSettings()
        {
            return localStorage.GetItem<AzureDevOps.ConnectionSettings>(LocalStorageKeys.AzureDevOpsSettings)
                ?? AzureDevOps.ConnectionSettings.Default;
        }

        public Kimai.ConnectionSettings GetKimaiSettings()
        {
            return localStorage.GetItem<Kimai.ConnectionSettings>(LocalStorageKeys.KimaiSettings)
                   ?? Kimai.ConnectionSettings.Default;
        }

        public MessageQueues.ConnectionSettings GetMessageQueueSettings()
        {
            return localStorage.GetItem<MessageQueues.ConnectionSettings>(LocalStorageKeys.MessageQueueSettings)
                   ?? MessageQueues.ConnectionSettings.Default;
        }

        public void SetAzureDevOpsSettings(AzureDevOps.ConnectionSettings settings)
        {
            localStorage.SetItem(LocalStorageKeys.AzureDevOpsSettings, settings);
        }

        public void SetKimaiSettings(Kimai.ConnectionSettings settings)
        {
            localStorage.SetItem(LocalStorageKeys.KimaiSettings, settings);
        }

        public void SetMessageQueueSettings(MessageQueues.ConnectionSettings settings)
        {
            localStorage.SetItem(LocalStorageKeys.MessageQueueSettings, settings);
        }
    }
}

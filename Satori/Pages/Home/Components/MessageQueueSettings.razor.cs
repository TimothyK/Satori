using Microsoft.AspNetCore.Components;
using Satori.MessageQueues;
using Satori.Utilities;

namespace Satori.Pages.Home.Components
{
    public partial class MessageQueueSettings
    {
        private VisibleCssClass IsVisibleClass { get; set; } = VisibleCssClass.Hidden;

        [Parameter]
        public EventCallback OnSettingsChanged { get; set; }

        public void Show()
        {
            var settings = ConnectionSettingsStore.GetMessageQueueSettings();

            Enabled = settings.Enabled;
            Subdomain = settings.Subdomain;
            QueueName = settings.QueueName;
            KeyName = settings.KeyName;
            Key = settings.Key;
            Validate();

            IsVisibleClass = VisibleCssClass.Visible;
            StateHasChanged();
        }

        private async Task SaveAsync()
        {
            Validate();
            if (!FormIsValid)
            {
                return;
            }

            var settings = new ConnectionSettings
            {
                Enabled = Enabled,
#pragma warning disable CS8601 // Possible null reference assignment.  Verified by FormIsValid above
                Subdomain = Subdomain,
                QueueName = QueueName,
                KeyName = KeyName,
                Key = Key
#pragma warning restore CS8601 // Possible null reference assignment.
            };
            ConnectionSettingsStore.SetMessageQueueSettings(settings);

            IsVisibleClass = VisibleCssClass.Hidden;
            await OnSettingsChanged.InvokeAsync();
        }
        private void CancelAzureDevOps()
        {
            IsVisibleClass = VisibleCssClass.Hidden;
        }

        private bool Enabled { get; set; } = true;
        private string? Subdomain { get; set; }
        private string? SubdomainValidationErrorMessage { get; set; }
        private string? QueueName { get; set; }
        private string? QueueNameValidationErrorMessage { get; set; }
        private string? KeyName { get; set; }
        private string? KeyNameValidationErrorMessage { get; set; }
        private string? Key { get; set; }
        private string? KeyValidationErrorMessage { get; set; }
        private bool FormIsValid { get; set; }

        private void Validate()
        {
            FormIsValid = true;
            if (!Enabled)
            {
                SubdomainValidationErrorMessage = null;
                QueueNameValidationErrorMessage = null;
                KeyNameValidationErrorMessage = null;
                KeyValidationErrorMessage = null;
                return;
            }

            SubdomainValidationErrorMessage = GetRequiredValidationErrorMessage(Subdomain);
            QueueNameValidationErrorMessage = GetRequiredValidationErrorMessage(QueueName);
            KeyNameValidationErrorMessage = GetRequiredValidationErrorMessage(KeyName);
            KeyValidationErrorMessage = GetRequiredValidationErrorMessage(Key);

            FormIsValid = string.IsNullOrEmpty(SubdomainValidationErrorMessage)
                          && string.IsNullOrEmpty(QueueNameValidationErrorMessage)
                          && string.IsNullOrEmpty(KeyNameValidationErrorMessage)
                          && string.IsNullOrEmpty(KeyValidationErrorMessage);
        }

        private static string? GetRequiredValidationErrorMessage(string? value)
        {
            return string.IsNullOrEmpty(value) ? "Required" : null;
        }
    }
}

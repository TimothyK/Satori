using Microsoft.AspNetCore.Components;
using Satori.MessageQueues;
using Satori.Utilities;

namespace Satori.Pages.ConfigSettings
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
            HostName = settings.HostName;
            Port = settings.Port.ToString();
            PortalPort = settings.PortalPort.ToString();
            Path = settings.Path;
            UserName = settings.UserName;
            Password = settings.Password;
            Validate();

            IsVisibleClass = VisibleCssClass.Visible;
            StateHasChanged();
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(HostName) || string.IsNullOrEmpty(Path) || string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password))
            {
                return;
            }
            if (!int.TryParse(Port, out var port) || !int.TryParse(PortalPort, out var portalPort))
            {
                return;
            }
            
            var settings = new ConnectionSettings
            {
                Enabled = Enabled,
                HostName = HostName,
                Port = port,
                PortalPort = portalPort,
                Path = Path,
                UserName = UserName,
                Password = Password
            };
            ConnectionSettingsStore.SetMessageQueueSettings(settings);

            IsVisibleClass = VisibleCssClass.Hidden;
            OnSettingsChanged.InvokeAsync();
        }
        private void CancelAzureDevOps()
        {
            IsVisibleClass = VisibleCssClass.Hidden;
        }

        private bool Enabled { get; set; } = true;
        private string? HostName { get; set; }
        private string? HostNameValidationErrorMessage { get; set; }
        private string? Port { get; set; }
        private string? PortValidationErrorMessage { get; set; }
        private string? PortalPort { get; set; }
        private string? PortalPortValidationErrorMessage { get; set; }
        private string? Path { get; set; }
        private string? PathValidationErrorMessage { get; set; }
        private string? UserName { get; set; }
        private string? UserNameValidationErrorMessage { get; set; }
        private string? Password { get; set; }
        private string? PasswordValidationErrorMessage { get; set; }
        private bool FormIsValid { get; set; }

        private void Validate()
        {
            FormIsValid = true;
            if (!Enabled)
            {
                HostNameValidationErrorMessage = null;
                PortValidationErrorMessage = null;
                PortalPortValidationErrorMessage = null;
                PathValidationErrorMessage = null;
                UserNameValidationErrorMessage = null;
                PasswordValidationErrorMessage = null;
                return;
            }

            HostNameValidationErrorMessage = GetRequiredValidationErrorMessage(HostName);
            PortValidationErrorMessage = GetPortValidationErrorMessage(Port);
            PortalPortValidationErrorMessage = GetPortValidationErrorMessage(PortalPort);
            PathValidationErrorMessage = GetRequiredValidationErrorMessage(Path);
            UserNameValidationErrorMessage = GetRequiredValidationErrorMessage(UserName);
            PasswordValidationErrorMessage = GetRequiredValidationErrorMessage(Password);

            FormIsValid = string.IsNullOrEmpty(HostNameValidationErrorMessage)
                          && string.IsNullOrEmpty(PortValidationErrorMessage)
                          && string.IsNullOrEmpty(PortalPortValidationErrorMessage)
                          && string.IsNullOrEmpty(PathValidationErrorMessage)
                          && string.IsNullOrEmpty(UserNameValidationErrorMessage)
                          && string.IsNullOrEmpty(PasswordValidationErrorMessage);
        }

        private static string? GetRequiredValidationErrorMessage(string? value)
        {
            return string.IsNullOrEmpty(value) ? "Required" : null;
        }
        
        private static string? GetPortValidationErrorMessage(string? value)
        {
            return !int.TryParse(value, out var port) ? "Numeric value required" 
                : port < 0 ? "Value must be positive" 
                : null;
        }

    }
}

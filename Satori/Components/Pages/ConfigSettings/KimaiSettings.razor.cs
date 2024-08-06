using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Satori.AzureDevOps.Models;
using Satori.Kimai;
using Satori.Utilities;

namespace Satori.Components.Pages.ConfigSettings
{
    public partial class KimaiSettings
    {
        private VisibleCssClass IsVisibleClass { get; set; } = VisibleCssClass.Hidden;

        [Parameter]
        public EventCallback OnSettingsChanged { get; set; }

        public void Show()
        {
            var settings = ConnectionSettingsStore.GetKimaiSettings();

            Enabled = settings.Enabled;
            Url = settings.Url.ToString();
            UserName = settings.UserName;
            ApiPassword = settings.ApiPassword;
            Validate();

            IsVisibleClass = VisibleCssClass.Visible;
            StateHasChanged();
        }

        private void Save()
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var url))
            {
                return;
            }
            if (string.IsNullOrEmpty(UserName))
            {
                return;
            }
            if (string.IsNullOrEmpty(ApiPassword))
            {
                return;
            }

            var settings = new ConnectionSettings
            {
                Enabled = Enabled,
                Url = url,
                UserName = UserName,
                ApiPassword = ApiPassword
            };
            ConnectionSettingsStore.SetKimaiSettings(settings);

            IsVisibleClass = VisibleCssClass.Hidden;
            OnSettingsChanged.InvokeAsync();
        }
        private void CancelAzureDevOps()
        {
            IsVisibleClass = VisibleCssClass.Hidden;
        }

        private bool Enabled { get; set; } = true;
        private string? Url { get; set; }
        private string? UrlValidationErrorMessage { get; set; }
        private string? UserName { get; set; }
        private string? UserNameValidationErrorMessage { get; set; }
        private string? ApiPassword { get; set; }
        private string? TokenValidationErrorMessage { get; set; }
        private bool FormIsValid { get; set; }

        private void Validate()
        {
            FormIsValid = true;
            if (!Enabled)
            {
                UrlValidationErrorMessage = null;
                UserNameValidationErrorMessage = null;
                TokenValidationErrorMessage = null;
                return;
            }

            UrlValidationErrorMessage = GetUrlValidationErrorMessage();
            UserNameValidationErrorMessage = GetUserNameValidationErrorMessage();
            TokenValidationErrorMessage = GetApiPasswordValidationErrorMessage();

            FormIsValid = string.IsNullOrEmpty(UrlValidationErrorMessage)
                          && string.IsNullOrEmpty(UserNameValidationErrorMessage) 
                          && string.IsNullOrEmpty(TokenValidationErrorMessage);
        }

        private string? GetUrlValidationErrorMessage()
        {
            if (string.IsNullOrEmpty(Url))
            {
                return "Required";
            }

            if (!Uri.TryCreate(Url, UriKind.Absolute, out var url))
            {
                return "Not a value URL";
            }

            if (url.Scheme.IsNotIn("https", "http"))
            {
                return "Should start with https://";
            }

            if (url.PathAndQuery != "/")
            {
                Url = url.GetLeftPart(UriPartial.Authority);
            }

            return null;
        }

        private string? GetUserNameValidationErrorMessage()
        {
            return string.IsNullOrEmpty(UserName) ? "Required" : null;
        }
        
        private string? GetApiPasswordValidationErrorMessage()
        {
            return string.IsNullOrEmpty(ApiPassword) ? "Required" : null;
        }

    }
}

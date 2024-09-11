using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Satori.AzureDevOps;
using Satori.Utilities;

namespace Satori.Pages.ConfigSettings
{
    public partial class AzureDevOpsSettings
    {
        private VisibleCssClass IsVisibleClass { get; set; } = VisibleCssClass.Hidden;

        [Parameter]
        public EventCallback OnSettingsChanged { get; set; }

        public void Show()
        {
            var settings = ConnectionSettingsStore.GetAzureDevOpsSettings();

            Enabled = settings.Enabled;
            Url = settings.Url.ToString();
            Token = settings.PersonalAccessToken;
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
            if (string.IsNullOrEmpty(Token))
            {
                return;
            }

            var settings = new ConnectionSettings
            {
                Enabled = Enabled,
                Url = url,
                PersonalAccessToken = Token
            };
            ConnectionSettingsStore.SetAzureDevOpsSettings(settings);

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
        private string? Token { get; set; }
        private string? TokenValidationErrorMessage { get; set; }
        private bool FormIsValid { get; set; }

        private void Validate()
        {
            FormIsValid = true;
            if (!Enabled)
            {
                UrlValidationErrorMessage = null;
                TokenValidationErrorMessage = null;
                return;
            }

            UrlValidationErrorMessage = GetUrlValidationErrorMessage();
            TokenValidationErrorMessage = GetTokenValidationErrorMessage();

            FormIsValid = string.IsNullOrEmpty(UrlValidationErrorMessage)
                                     && string.IsNullOrEmpty(TokenValidationErrorMessage);
        }

        private string? GetUrlValidationErrorMessage()
        {
            Url = Url?.TrimEnd('/');

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

            if (url.PathAndQuery == "/")
            {
                return "Missing the Organization in the path";
            }

            if (url.PathAndQuery.Count(c => c == '/') > 1)
            {
                return "Expecting just the Organization in the path";
            }

            return null;
        }

        private string? GetTokenValidationErrorMessage()
        {
            return string.IsNullOrEmpty(Token) ? "Required" : null;
        }

    }
}

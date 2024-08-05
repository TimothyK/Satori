using CodeMonkeyProjectiles.Linq;
using Satori.AzureDevOps;
using Satori.Utilities;

namespace Satori.Components.Pages
{
    public partial class AzureDevOpsSettings
    {
        private VisibleCssClass AzureDevopsVisible { get; set; } = VisibleCssClass.Hidden;

        public void ShowAzureDevOps()
        {
            var settings = ConnectionSettingsStore.GetAzureDevOpsSettings();

            AzureDevOpsEnabled = settings.Enabled;
            AzureDevOpsUrl = settings.Url.ToString();
            AzureDevOpsToken = settings.PersonalAccessToken;
            ValidateAzureDevOps();

            AzureDevopsVisible = VisibleCssClass.Visible;
            StateHasChanged();
        }

        private void SaveAzureDevOps()
        {
            if (!Uri.TryCreate(AzureDevOpsUrl, UriKind.Absolute, out var url))
            {
                return;
            }
            if (string.IsNullOrEmpty(AzureDevOpsToken))
            {
                return;
            }

            var settings = new ConnectionSettings
            {
                Enabled = AzureDevOpsEnabled,
                Url = url,
                PersonalAccessToken = AzureDevOpsToken
            };
            ConnectionSettingsStore.SetAzureDevOpsSettings(settings);

            AzureDevopsVisible = VisibleCssClass.Hidden;
        }
        private void CancelAzureDevOps()
        {
            AzureDevopsVisible = VisibleCssClass.Hidden;
        }

        private bool AzureDevOpsEnabled { get; set; } = true;
        private string? AzureDevOpsUrl { get; set; }
        private string? AzureDevOpsUrlValidationErrorMessage { get; set; }
        private string? AzureDevOpsToken { get; set; }
        private string? AzureDevOpsTokenValidationErrorMessage { get; set; }
        private bool AzureDevOpsFormIsValid { get; set; }

        private void ValidateAzureDevOps()
        {
            AzureDevOpsFormIsValid = true;
            if (!AzureDevOpsEnabled)
            {
                AzureDevOpsUrlValidationErrorMessage = null;
                AzureDevOpsTokenValidationErrorMessage = null;
                return;
            }

            AzureDevOpsUrlValidationErrorMessage = GetAzureDevOpsUrlValidationErrorMessage();
            AzureDevOpsTokenValidationErrorMessage = GetAzureDevOpsTokenValidationErrorMessage();

            AzureDevOpsFormIsValid = string.IsNullOrEmpty(AzureDevOpsUrlValidationErrorMessage)
                                     && string.IsNullOrEmpty(AzureDevOpsTokenValidationErrorMessage);
        }

        private string? GetAzureDevOpsUrlValidationErrorMessage()
        {
            AzureDevOpsUrl = AzureDevOpsUrl?.TrimEnd('/');

            if (string.IsNullOrEmpty(AzureDevOpsUrl))
            {
                return "Required";
            }

            if (!Uri.TryCreate(AzureDevOpsUrl, UriKind.Absolute, out var url))
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

        private string? GetAzureDevOpsTokenValidationErrorMessage()
        {
            return string.IsNullOrEmpty(AzureDevOpsToken) ? "Required" : null;
        }

    }
}

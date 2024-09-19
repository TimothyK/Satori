using CodeMonkeyProjectiles.Linq;
using Microsoft.AspNetCore.Components;
using Satori.Kimai;
using Satori.Utilities;

namespace Satori.Pages.ConfigSettings;

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
        ApiToken = settings.ApiToken;
        UserName = settings.UserName;
        ApiPassword = settings.ApiPassword;
        SetAuthenticationMethod(settings.AuthenticationMethod);
        Validate();

        IsVisibleClass = VisibleCssClass.Visible;
        StateHasChanged();
    }

    private void Save()
    {
        Validate();
        if (!FormIsValid)
        {
            return;
        }
        var url = new Uri(Url ?? throw new InvalidOperationException());

        var settings = new ConnectionSettings
        {
            Enabled = Enabled,
            Url = url,
            AuthenticationMethod = AuthenticationMethod,
            ApiToken = ApiToken,
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
    private KimaiAuthenticationMethod AuthenticationMethod { get; set; } = KimaiAuthenticationMethod.Token;

    private void SetAuthenticationMethod(KimaiAuthenticationMethod value)
    {
        AuthenticationMethod = value;
        ShowToken = value == KimaiAuthenticationMethod.Token;
        ShowPassword = value == KimaiAuthenticationMethod.Password;

        Validate();
    }

    private VisibleCssClass ShowToken { get; set; } = VisibleCssClass.Visible;
    private VisibleCssClass ShowPassword { get; set; } = VisibleCssClass.Hidden;

    private string? ApiToken { get; set; }
    private string? TokenValidationErrorMessage { get; set; }
    private string? UserName { get; set; }
    private string? UserNameValidationErrorMessage { get; set; }
    private string? ApiPassword { get; set; }
    private string? PasswordValidationErrorMessage { get; set; }
    private bool FormIsValid { get; set; }

    private void Validate()
    {
        FormIsValid = true;
        if (!Enabled)
        {
            UrlValidationErrorMessage = null;
            TokenValidationErrorMessage = null;
            UserNameValidationErrorMessage = null;
            PasswordValidationErrorMessage = null;
            return;
        }

        UrlValidationErrorMessage = GetUrlValidationErrorMessage();
        TokenValidationErrorMessage = GetRequiredValidationErrorMessage(KimaiAuthenticationMethod.Token, ApiToken);
        UserNameValidationErrorMessage = GetRequiredValidationErrorMessage(KimaiAuthenticationMethod.Password, UserName);
        PasswordValidationErrorMessage = GetRequiredValidationErrorMessage(KimaiAuthenticationMethod.Password, ApiPassword);

        FormIsValid = string.IsNullOrEmpty(UrlValidationErrorMessage)
                      && string.IsNullOrEmpty(TokenValidationErrorMessage)
                      && string.IsNullOrEmpty(UserNameValidationErrorMessage) 
                      && string.IsNullOrEmpty(PasswordValidationErrorMessage);
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

    private string? GetRequiredValidationErrorMessage(KimaiAuthenticationMethod targetAuthMethod, string? value)
    {
        if (AuthenticationMethod != targetAuthMethod)
        {
            return null;
        }
        return string.IsNullOrEmpty(value) ? "Required" : null;
    }
}
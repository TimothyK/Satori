using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels.AlertServices;
using Shouldly;

namespace Satori.AppServices.Tests.TestDoubles.AlertServices;

public class TestAlertService : IAlertService
{
    public void BroadcastAlert(Exception ex)
    {
        LastException = ex;
        LastMessage = ex.Message;
    }

    public void BroadcastAlert(string message)
    {
        if (!VerificationEnabled) return;

        LastException = null;
        LastMessage = message;
    }

    public void ClearAlert()
    {
        LastException = null;
        LastMessage = null;
    }

    public AlertViewModel Subscribe() => throw new NotSupportedException();

    public Exception? LastException { get; set; }
    public string? LastMessage { get; set; }

    private bool VerificationEnabled { get; set; } = true;

    public void DisableVerifications()
    {
        VerificationEnabled = false;
    }

    public void VerifyNoMessagesWereBroadcast()
    {
        if (!VerificationEnabled)
        {
            return;
        }

        LastException.ShouldBeNull();
        LastMessage.ShouldBeNull();
    }
}
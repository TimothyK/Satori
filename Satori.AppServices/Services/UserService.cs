using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.Kimai;
using User = Satori.Kimai.Models.User;

namespace Satori.AppServices.Services;

public class UserService(
    IAzureDevOpsServer azureDevOpsServer
    , IKimaiServer kimaiServer
    , IAlertService alertService
)
{
    public async Task<Person> GetCurrentUserAsync()
    {
        if (Person.Me != null)
        {
            return Person.Me;
        }
        if (!azureDevOpsServer.Enabled && !kimaiServer.Enabled)
        {
            return Person.Empty;
        }

        User? kimaiUser = null;
        if (kimaiServer.Enabled)
        {
            try
            {
                kimaiUser = await kimaiServer.GetMyUserAsync();
            }
            catch (Exception ex)
            {
                alertService.BroadcastAlert(ex);
            }
        }

        Identity? identity = null;
        if (azureDevOpsServer.Enabled)
        {
            try
            {
                var azureDevOpsId = await azureDevOpsServer.GetCurrentUserIdAsync();
                identity = await azureDevOpsServer.GetIdentityAsync(azureDevOpsId);
            }
            catch (Exception ex)
            {
                alertService.BroadcastAlert(ex);
            }
        }
        var connectionSettings = azureDevOpsServer.ConnectionSettings;


        if (identity == null && kimaiUser == null)
        {
            return Person.Empty;
        }

        return Person.Me = Person.From(identity, kimaiUser, connectionSettings);
    }
}

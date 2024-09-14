using Satori.AppServices.ViewModels;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using Satori.Kimai;

namespace Satori.AppServices.Services;

public class UserService(IAzureDevOpsServer azureDevOpsServer, IKimaiServer kimaiServer)
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

        var kimaiUser = !kimaiServer.Enabled ? null : await kimaiServer.GetMyUserAsync();

        Identity? identity = null;
        if (azureDevOpsServer.Enabled)
        {
            var azureDevOpsId = await azureDevOpsServer.GetCurrentUserIdAsync();
            identity = await azureDevOpsServer.GetIdentityAsync(azureDevOpsId);
        }
        var connectionSettings = azureDevOpsServer.ConnectionSettings;

        return Person.Me = Person.From(identity, kimaiUser, connectionSettings);
    }
}

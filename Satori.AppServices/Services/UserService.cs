using Satori.AppServices.ViewModels;
using Satori.AzureDevOps;
using Satori.Kimai;

namespace Satori.AppServices.Services;

public class UserService(IAzureDevOpsServer azureDevOpsServer, IKimaiServer kimaiServer)
{
    public async Task<Person> GetCurrentUserAsync()
    {
        var azureDevOpsId = await azureDevOpsServer.GetCurrentUserIdAsync();
        var identity = await azureDevOpsServer.GetIdentityAsync(azureDevOpsId);
        var connectionSettings = azureDevOpsServer.ConnectionSettings;

        var kimaiUser = await kimaiServer.GetMyUserAsync();

        return Person.From(identity, kimaiUser, connectionSettings);
    }
}

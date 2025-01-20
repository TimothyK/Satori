using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;

public static class IdentityExtensions
{
    public static User ToUser(this Identity identity)
    {
        return new User
        {
            Id = identity.Id,
            DisplayName = identity.ProviderDisplayName,
            ImageUrl = "https://azureDevOps.test/Org/Id?id=" + identity.Id,
            UniqueName = $"{identity.Properties.Domain}\\{identity.Properties.Account}",
            Url = "https://azureDevOps.test/Org/Id?id=" + identity.Id,
        };
    }
}
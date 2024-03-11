using Satori.AppServices.ViewModels;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Services.Converters;

internal static class UserExtensions
{
    public static Person? ToNullableViewModel(this User? user) => user == null ? null : ToViewModel(user);

    public static Person ToViewModel(this User user)
    {
        return new Person()
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = user.ImageUrl,
        };
    }

}
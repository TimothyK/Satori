using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Kimai;

internal static class People
{
    public static readonly User Alice = BuildUser(nameof(Alice));
    public static readonly User Bob = BuildUser(nameof(Bob));
    public static readonly User Cathy = BuildUser(nameof(Cathy));
    public static readonly User Dave = BuildUser(nameof(Dave));
    //public static readonly User Emily = BuildUser(nameof(Emily));
    //public static readonly User Fred = BuildUser(nameof(Fred));

    private static User BuildUser(string name)
    {
        var user = Builder.Builder<User>.New().Build(int.MaxValue);

        user.DisplayName = name;
        user.UniqueName = name;
        user.ImageUrl = $"http://devops.test/Org/_api/_common/identityImage?id={user.Id}";

        return user;
    }

}
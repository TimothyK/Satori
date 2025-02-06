using Builder;
using Satori.Kimai.Models;

namespace Satori.AppServices.Tests.TestDoubles.Kimai;

public static class KimaiUserBuilder
{
    public static User BuildUser() => Builder<User>.New().Build(user =>
    {
        user.Id = Sequence.KimaiUserId.Next();
        user.UserName = $"User {user.Id}";
        user.Enabled = true;
        user.Language = "en_CA";
    });
}
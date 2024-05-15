using Flurl;

namespace Satori.AppServices.ViewModels;

public class Person
{
    private Person()
    {
    }

    #region Properties

    public Guid AzureDevOpsId { get; private init; }
    public required string DisplayName { get; init; }
    public required Uri AvatarUrl { get; init; }
    public string? EmailAddress { get; private init; }
    public string? DomainLogin { get; private init; }
    public int? KimaiId { get; private init; }

    #endregion Properties

    #region Null Object

    public static readonly Person Null = new()
    {
        AzureDevOpsId = Guid.Empty,
        DisplayName = "Unknown/Unassigned",
        AvatarUrl = new Url("/images/NullAvatar.png").ToUri(),
    };

    public bool IsNull => AzureDevOpsId == Guid.Empty;

    #endregion Null Object

    #region Caching

    private static Person FromAzureDevOpsId(Guid id, Func<Person> createPerson)
    {
        lock (PeopleLock)
        {
            var person = People.GetValueOrDefault(id);
            if (person != null)
            {
                return person;
            }

            person = createPerson();
            People[id] = person;
            return person;
        }
    }

    private static readonly object PeopleLock = new();
    private static readonly Dictionary<Guid, Person> People = [];

    #endregion Caching

    #region Casting

    public static implicit operator Person(AzureDevOps.Models.User? user)
    {
        return user == null ? Null 
            : FromAzureDevOpsId(user.Id, CreatePerson);

        Person CreatePerson()
        {
            return new Person()
            {
                AzureDevOpsId = user.Id,
                DisplayName = user.DisplayName,
                AvatarUrl = new Uri(user.ImageUrl),
                DomainLogin = user.UniqueName,
            };
        }
    }

    public static Person From(AzureDevOps.Models.Identity azDoIdentity, Kimai.Models.User kimaiUser, AzureDevOps.ConnectionSettings azDoSettings)
    {
        lock (PeopleLock)
        {
            var cache = People.GetValueOrDefault(azDoIdentity.Id);
            if (cache != null && cache.KimaiId == kimaiUser.Id)
            {
                return cache;
            }
            if (cache != null)
            {
                People.Remove(cache.AzureDevOpsId);
            }
            return FromAzureDevOpsId(azDoIdentity.Id, CreatePerson);
        }

        Person CreatePerson()
        {
            return new Person()
            {
                AzureDevOpsId = azDoIdentity.Id,
                DisplayName = azDoIdentity.ProviderDisplayName,
                AvatarUrl = kimaiUser.Avatar ?? azDoSettings.Url.AppendPathSegment("_api/_common/identityImage").AppendQueryParam("id", azDoIdentity.Id).ToUri(),
                EmailAddress = azDoIdentity.Properties.Mail?.Value,
                KimaiId = kimaiUser.Id,
                DomainLogin = $@"{azDoIdentity.Properties.Domain?.Value}\{azDoIdentity.Properties.Account?.Value}",
            };
        }
    }

    #endregion Casting
}
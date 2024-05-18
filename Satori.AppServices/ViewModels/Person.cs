using Flurl;
using KimaiUser = Satori.Kimai.Models.User;
using AzDoUser = Satori.AzureDevOps.Models.User;
using ConnectionSettings = Satori.AzureDevOps.ConnectionSettings;

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
    public DayOfWeek FirstDayOfWeek { get; private init; } = DayOfWeek.Monday;
    public string Language { get; private init; } = "en";

    #endregion Properties

    #region Null Object

    public static readonly Person Empty = new()
    {
        AzureDevOpsId = Guid.Empty,
        DisplayName = "Unknown/Unassigned",
        AvatarUrl = new Url("/images/NullAvatar.png").ToUri(),
    };

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

    public static implicit operator Person(AzDoUser? user)
    {
        return user == null ? Empty 
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

    public static Person From(AzureDevOps.Models.Identity azDoIdentity, KimaiUser kimaiUser, ConnectionSettings azDoSettings)
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
                FirstDayOfWeek = GetFirstDayOfWeek(kimaiUser),
                Language = kimaiUser.Language?.Replace("_", "-") ?? "en",
            };
        }
    }

    private static DayOfWeek GetFirstDayOfWeek(KimaiUser kimaiUser)
    {
        var firstDayOfWeekSetting = kimaiUser.Preferences?.SingleOrDefault(p => p.Name == "firstDayOfWeek")?.Value;
        if (!Enum.TryParse(firstDayOfWeekSetting, ignoreCase: true, out DayOfWeek firstDayOfWeek))
        {
            firstDayOfWeek = DayOfWeek.Monday;
        }
        return firstDayOfWeek;
    }

    #endregion Casting
}
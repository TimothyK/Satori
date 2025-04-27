using Flurl;
using Satori.AzureDevOps.Models;
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

    public override string ToString() => DisplayName;

    #endregion Properties

    #region Null Object

    public static readonly Person Empty = new()
    {
        AzureDevOpsId = Guid.Empty,
        DisplayName = "Unknown/Unassigned",
        AvatarUrl = new Url("/images/NullAvatar.png").ToUri(),
    };

    public static readonly Person Anyone = new()
    {
        AzureDevOpsId = Guid.NewGuid(),
        DisplayName = "Anyone",
        AvatarUrl = new Url("/images/AllAvatar.png").ToUri(),
    };

    #endregion Null Object

    #region Caching

    public static Person? Me { get; set; }

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

    /// <summary>
    /// Cache of Person objects.  The Key of this dictionary is from <see cref="GetUserId"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cache is thread safe.  Use the <see cref="PeopleLock"/> to synchronize access to this cache.
    /// </para>
    /// </remarks>
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

    public static Person From(Identity? azDoIdentity, KimaiUser? kimaiUser, ConnectionSettings azDoSettings)
    {
        if (azDoIdentity == null && kimaiUser == null)
        {
            throw new ArgumentNullException(nameof(azDoIdentity), "Both the Azure DevOps and Kimai User cannot be null"); 
        } 

        lock (PeopleLock)
        {
            var id = GetUserId(azDoIdentity, kimaiUser);
            var cache = People.GetValueOrDefault(id);
            if (cache != null && cache.KimaiId == kimaiUser?.Id)
            {
                return cache;
            }
            if (cache != null)
            {
                People.Remove(cache.AzureDevOpsId);
            }
            return FromAzureDevOpsId(id, CreatePerson);
        }

        Person CreatePerson()
        {
            return new Person()
            {
                AzureDevOpsId = azDoIdentity?.Id ?? Guid.Empty,
                DisplayName = azDoIdentity?.ProviderDisplayName ?? kimaiUser?.Alias ?? "Unknown user",
                AvatarUrl = kimaiUser?.Avatar 
                            ?? (azDoIdentity == null ? new Url("/images/DefaultAvatar.png").ToUri() 
                                : azDoSettings.Url.AppendPathSegment("_api/_common/identityImage").AppendQueryParam("id", azDoIdentity.Id).ToUri()),
                EmailAddress = azDoIdentity?.Properties.Mail?.Value,
                KimaiId = kimaiUser?.Id,
                DomainLogin = azDoIdentity == null ? null : $@"{azDoIdentity.Properties.Domain?.Value}\{azDoIdentity.Properties.Account?.Value}",
                FirstDayOfWeek = GetFirstDayOfWeek(kimaiUser),
                Language = kimaiUser?.Language?.Replace("_", "-") ?? "en",
            };
        }
    }

    public static Person? FromDisplayName(string displayName)
    {
        lock (PeopleLock)
        {
            return People.Values.SingleOrDefault(p => p.DisplayName == displayName);
        }
    }

    /// <summary>
    /// Gets a unique ID for a user.  This id is used for caching of the Person.  See <see cref="People"/>.
    /// </summary>
    /// <param name="azDoIdentity"></param>
    /// <param name="kimaiUser"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// Azure DevOps and Kimai both have their own IDs for users.  AzDO uses a Guid, Kimai uses an int.
    /// Typically, this application with AzDO enabled, so every user will have an AzDO Guid.
    /// In the rare case that AzDO is disabled, the Kimai ID will be used instead.
    /// </para>
    /// <para>
    /// This probably isn't a great design.  Instead of converting the Kimai int32 ID to a GUID,
    /// it would better to have a Satori UserId class that holds both/all the foreign IDs.
    /// 
    /// </para>
    /// </remarks>
    private static Guid GetUserId(Identity? azDoIdentity, KimaiUser? kimaiUser)
    {
        return GetUserId(azDoIdentity?.Id, kimaiUser?.Id);
    }
    private static Guid GetUserId(Guid? azureUserId, int? kimaiUserId)
    {
        return azureUserId ?? IntToGuid(kimaiUserId);
    }

    private static Guid IntToGuid(int? value)
    {
        if (value == null)
        {
            return Guid.Empty;
        }

        var bytes = new byte[16];
        BitConverter.GetBytes(value.Value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    private static DayOfWeek GetFirstDayOfWeek(KimaiUser? kimaiUser)
    {
        if (kimaiUser == null)
        {
            return DayOfWeek.Monday;
        }
        
        var firstDayOfWeekSetting = kimaiUser.Preferences?.SingleOrDefault(p => p.Name == "firstDayOfWeek")?.Value;
        if (!Enum.TryParse(firstDayOfWeekSetting, ignoreCase: true, out DayOfWeek firstDayOfWeek))
        {
            firstDayOfWeek = DayOfWeek.Monday;
        }
        return firstDayOfWeek;
    }

    #endregion Casting

    #region Equality

    public override int GetHashCode()
    {
        return GetUserId(AzureDevOpsId, KimaiId).GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Person)obj);
    }

    protected bool Equals(Person other)
    {
        if (KimaiId != null && other.KimaiId != null)
        {
            return KimaiId == other.KimaiId;
        }
        return AzureDevOpsId == other.AzureDevOpsId;
    }

    // Equality operator overloads
    public static bool operator ==(Person? left, Person? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null || right is null)
        {
            return false;
        }
        return GetUserId(left.AzureDevOpsId, left.KimaiId) == GetUserId(right.AzureDevOpsId, right.KimaiId);
    }

    public static bool operator !=(Person? left, Person? right)
    {
        return !(left == right);
    }

    #endregion Equality
}
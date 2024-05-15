using Flurl;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.ViewModels;

public class Person
{
    private Person()
    {
    }

    #region Properties

    public Guid AzureDevOpsId { get; init; }
    public required string DisplayName { get; init; }
    public required Uri AvatarUrl { get; init; }
    public string? JobTitle { get; set; }
    public string? EmailAddress { get; set; }
    public string? DomainLogin { get; set; }
    public int? KimaiId { get; set; }

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

    public static implicit operator Person(User? user)
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

    #endregion Casting
}
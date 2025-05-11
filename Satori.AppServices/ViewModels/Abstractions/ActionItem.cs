namespace Satori.AppServices.ViewModels.Abstractions;

public abstract class ActionItem(string actionDescription, params Person[] people)
{
    public List<PersonPriority> On { get; set; } = 
        people
            .Select(person => new PersonPriority(person))
            .ToList();

    /// <summary>
    /// This should be a very short (perhaps a single verb) to describe what action should be taken.
    /// E.g "Start", "Finish", "Reply", "Publish", "Review", etc.
    /// </summary>
    public string ActionDescription { get; } = actionDescription;
}

public class PersonPriority(Person person)
{
    public Person Person { get; } = person;
    public int Priority { get; set; }
}
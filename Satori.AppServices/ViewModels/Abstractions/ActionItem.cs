namespace Satori.AppServices.ViewModels.Abstractions;

public abstract class ActionItem(string actionDescription, params Person[] people)
{
    public List<Person> On { get; set; } = people.ToList();

    /// <summary>
    /// This should be a very short (perhaps a single verb) to describe what action should be taken.
    /// E.g "Start", "Finish", "Reply", "Publish", "Review", etc.
    /// </summary>
    public string ActionDescription { get; } = actionDescription;
}
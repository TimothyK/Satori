namespace Satori.AppServices.ViewModels.Abstractions;

public abstract class ActionItem
{
    protected ActionItem(string message, params Person[] people)
    {
        On = people.ToList();
        Message = message;
    }

    public List<Person> On { get; set; } = [];

    public string Message { get; }
}
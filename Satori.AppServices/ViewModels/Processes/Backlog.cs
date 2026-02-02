namespace Satori.AppServices.ViewModels.Processes;

public class Backlog
{
    public required Process Process { get; set; }

    public required string Id { get; set; }
    public required string Name { get; set; }

    public BacklogType Type { get; set; }
}
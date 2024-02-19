namespace Satori.AzureDevOps.Models;

public class Iteration
{
    public Guid id { get; set; }
    public string name { get; set; }
    public string path { get; set; }
    public Attributes attributes { get; set; }
    public string url { get; set; }
}

public class Attributes
{
    public DateTimeOffset? startDate { get; set; }
    public DateTimeOffset? finishDate { get; set; }
    public string timeFrame { get; set; }
}
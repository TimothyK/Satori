namespace Satori.AzureDevOps.Models;

public class Team
{
    public Guid id { get; set; }
    public string name { get; set; }
    public string url { get; set; }
    public string description { get; set; }
    public string identityUrl { get; set; }
    public string projectName { get; set; }
    public string projectId { get; set; }
}
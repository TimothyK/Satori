namespace Satori.AzureDevOps.Models;

public class Repository
{
    public string id { get; set; }
    public string name { get; set; }
    public Project project { get; set; }
    public string url { get; set; }
}
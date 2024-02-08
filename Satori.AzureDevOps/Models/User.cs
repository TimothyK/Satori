namespace Satori.AzureDevOps.Models;

public class User
{
    public Links _links { get; set; }
    public string descriptor { get; set; }
    public string displayName { get; set; }
    public Guid id { get; set; }
    public string imageUrl { get; set; }
    public string uniqueName { get; set; }
    public string url { get; set; }
}
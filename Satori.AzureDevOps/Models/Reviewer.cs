namespace Satori.AzureDevOps.Models;

public class Reviewer : User
{
    public bool hasDeclined { get; set; }
    public bool isFlagged { get; set; }
    public bool isRequired { get; set; }
    public string reviewerUrl { get; set; }
    public int vote { get; set; }
}
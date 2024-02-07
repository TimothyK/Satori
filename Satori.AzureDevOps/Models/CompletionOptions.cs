namespace Satori.AzureDevOps.Models;

public class CompletionOptions
{
    public object[] autoCompleteIgnoreConfigIds { get; set; }
    public string mergeStrategy { get; set; }
    public bool deleteSourceBranch { get; set; }
    public string mergeCommitMessage { get; set; }
    public bool transitionWorkItems { get; set; }
}
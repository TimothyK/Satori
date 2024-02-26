using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles;

internal interface IBuilderAccess
{
    void AddPullRequest(PullRequest pullRequest);
    void LinkWorkItem(PullRequest pullRequest, WorkItem workItem);
}
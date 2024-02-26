using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Database;

internal interface IAzureDevOpsDatabaseWriter
{
    void AddPullRequest(PullRequest pullRequest);
    void LinkWorkItem(PullRequest pullRequest, WorkItem workItem);
}
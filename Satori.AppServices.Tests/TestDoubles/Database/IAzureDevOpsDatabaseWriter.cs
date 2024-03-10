using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Database;

internal interface IAzureDevOpsDatabaseWriter
{
    void AddPullRequest(PullRequest pullRequest);
    void LinkWorkItem(PullRequest pullRequest, WorkItem workItem);
    void AddTeam(Team team);
    void LinkIteration(Team team, Iteration iteration);
}
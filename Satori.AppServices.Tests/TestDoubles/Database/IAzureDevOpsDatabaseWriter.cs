using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps.Models;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.TestDoubles.Database;

internal interface IAzureDevOpsDatabaseWriter
{
    void AddPullRequest(PullRequest pullRequest);
    void LinkWorkItem(PullRequest pullRequest, WorkItem workItem);
    void AddTeam(Team team);
    void LinkIteration(Team team, Iteration iteration);
    void AddWorkItem(WorkItem workItem);
    void AddWorkItemLink(WorkItem leftWorkItem, LinkType linkType, WorkItem rightWorkItem);
}
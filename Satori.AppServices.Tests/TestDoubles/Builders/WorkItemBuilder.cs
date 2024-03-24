using Satori.AppServices.Tests.TestDoubles.Database;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AzureDevOps.Models;

namespace Satori.AppServices.Tests.TestDoubles.Builders;

internal class WorkItemBuilder
{
    public WorkItemBuilder(IAzureDevOpsDatabaseWriter database)
    {
        WorkItem = BuildWorkItem();
        database.AddWorkItem(WorkItem);
    }

    public WorkItem WorkItem { get; }

    private static WorkItem BuildWorkItem()
    {
        var workItem = Builder.Builder<WorkItem>.New().Build(int.MaxValue);
        workItem.Url = $"http://devops.test/Org/{workItem.Fields.ProjectName}/_apis/wit/workItems/{workItem.Id}";
        return workItem;
    }

    public WorkItemBuilder WithSprint(Sprint sprint)
    {
        WorkItem.Fields.ProjectName = sprint.ProjectName;
        WorkItem.Fields.IterationPath = sprint.IterationPath;

        return this;

    }
}
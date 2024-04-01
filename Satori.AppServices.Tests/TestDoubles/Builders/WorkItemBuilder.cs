using Pscl.Linq;
using Satori.AppServices.Tests.TestDoubles.Database;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.TestDoubles.Builders;

internal class WorkItemBuilder
{
    private readonly IAzureDevOpsDatabaseWriter _database;

    public WorkItemBuilder(IAzureDevOpsDatabaseWriter database)
    {
        _database = database;
        WorkItem = BuildWorkItem();
        database.AddWorkItem(WorkItem);
    }

    public WorkItem WorkItem { get; }

    private static WorkItem BuildWorkItem()
    {
        var workItem = Builder.Builder<WorkItem>.New().Build(int.MaxValue);
        workItem.Fields.WorkItemType = new[] { WorkItemType.ProductBacklogItem, WorkItemType.Bug }.SingleRandom().ToApiValue();
        workItem.Fields.State = ScrumState.Committed.ToApiValue();
        workItem.Fields.Triage = null;
        workItem.Url = $"http://devops.test/Org/{workItem.Fields.ProjectName}/_apis/wit/workItems/{workItem.Id}";
        return workItem;
    }

    public WorkItemBuilder WithSprint(Sprint sprint)
    {
        WorkItem.Fields.ProjectName = sprint.ProjectName;
        WorkItem.Fields.IterationPath = sprint.IterationPath;
        return this;
    }

    public WorkItemBuilder AddChild(out WorkItem child)
    {
        child = BuildWorkItem();

        child.Fields.WorkItemType = GetChildType(WorkItem.Fields.WorkItemType).ToApiValue();

        child.Fields.ProjectName = WorkItem.Fields.ProjectName;
        child.Fields.IterationPath = WorkItem.Fields.IterationPath;
        
        _database.AddWorkItem(child);
        _database.AddWorkItemLink(WorkItem, LinkType.IsParentOf, child);

        return this;
    }

    private static WorkItemType GetChildType(string workItemTypeApiValue) => GetChildType(WorkItemType.FromApiValue(workItemTypeApiValue));
    private static WorkItemType GetChildType(WorkItemType workItemType)
    {
        var boardTypes = new[] { WorkItemType.ProductBacklogItem, WorkItemType.Bug };
        if (workItemType.IsIn(boardTypes))
        {
            return WorkItemType.Task;
        }
        if (workItemType == WorkItemType.Feature)
        {
            return boardTypes.SingleRandom();
        }
        if (workItemType == WorkItemType.Epic)
        {
            return WorkItemType.Feature;
        }
        throw new InvalidOperationException("The parent type cannot add a child");
    }
}
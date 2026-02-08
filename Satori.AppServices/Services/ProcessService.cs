using Satori.AppServices.ViewModels.Processes;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using State = Satori.AppServices.ViewModels.Processes.State;
using WorkItemType = Satori.AppServices.ViewModels.Processes.WorkItemType;

namespace Satori.AppServices.Services;

public class ProcessService(IAzureDevOpsServer azureDevOpsServer)
{
    private readonly Dictionary<string, Process> _projectProcess = [];

    public async Task<State> GetStateAsync(WorkItem workItem)
    {
        var projectName = workItem.Fields.ProjectName;
        if (!_projectProcess.TryGetValue(projectName, out var process))
        {
            _projectProcess[projectName] = process = await LoadProcessAsync(projectName);
        };

        var type = GetWorkItemType(process, workItem);
        return type.States[workItem.Fields.State];
    }

    private static WorkItemType GetWorkItemType(Process process, WorkItem workItem)
    {
        var workItemTypeName = workItem.Fields.WorkItemType;
        if (process.WorkItemTypes.TryGetValue(workItemTypeName, out var type))
        {
            return type;
        }

        //TODO: Remove after the stub is implemented.  The WorkItemTypesDictionary will always have the value.
        type = new WorkItemType
        {
            Name = workItemTypeName,
        };

        var state =  new State
        {
            Name = workItem.Fields.State,
            WorkItemType = type,
        };
        type.States.Add(state.Name, state);

        process.WorkItemTypes[workItemTypeName] = type;
        return type;
    }


    private Project[]? _projects;

    private async Task<Process> LoadProcessAsync(string projectName)
    {
        var projects = _projects ??= await azureDevOpsServer.GetProjectsAsync();
        var projectId = projects.First(p => p.Name == projectName);

        //TODO: 
        // Lookup the project properties. https://dev.azure.com/{Org}/_apis/projects/{projectId}/properties?api-version=7.1-preview.1
        //   to get the processId (name=-System.ProcessTemplateType)
        // Lookup the work item types by the processId. https://dev.azure.com/{Org}/_apis/work/processes/{processId}/workitemtypes?api-version=7.1
        // Lookup the states for each work item type. https://dev.azure.com/{Org}/_apis/work/processes/{processId}/workItemTypes/{workItemType.ReferenceName}/states?api-version=7.1
        // Lookup the backlog configuration by the projectName. https://dev.azure.com/{Org}/{Project}/_apis/work/backlogconfiguration?api-version=7.1

        return new Process
        {
            Name = "foo",
        };
    }
}
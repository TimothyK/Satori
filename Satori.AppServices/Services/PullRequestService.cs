using Flurl;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.PullRequests;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.AzureDevOps.Models;
using PullRequest = Satori.AppServices.ViewModels.PullRequests.PullRequest;
using PullRequestDto = Satori.AzureDevOps.Models.PullRequest;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services;

public class PullRequestService(IAzureDevOpsServer azureDevOpsServer)
{
    private IAzureDevOpsServer AzureDevOpsServer { get; } = azureDevOpsServer;
    private ConnectionSettings ConnectionSettings => AzureDevOpsServer.ConnectionSettings;

    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
    {
        var pullRequests = await AzureDevOpsServer.GetPullRequestsAsync();

        var workItemMap = new Dictionary<int, List<int>>();
        foreach (var pr in pullRequests)
        {
            var idMap = await AzureDevOpsServer.GetPullRequestWorkItemIdsAsync(pr);
            workItemMap.Add(pr.PullRequestId, idMap.Select(x => x.Id).ToList());
        }

        var workItemIds = workItemMap.SelectMany(kvp => kvp.Value).Distinct();
        var workItems = (await AzureDevOpsServer.GetWorkItemsAsync(workItemIds))
            .ToDictionary(wi => wi.Id, ToViewModel);

        var viewModels = pullRequests.Select(ToViewModel).ToArray();
        foreach (var pr in viewModels)
        {
            pr.WorkItems = workItemMap[pr.Id].Select(workItemId => workItems[workItemId]).ToList();
        }

        return viewModels;
    }

    private WorkItem ToViewModel(AzureDevOps.Models.WorkItem wi)
    {
        var id = wi.Id;
        var workItem = new WorkItem()
        {
            Id = id,
            Title = wi.Fields.Title,
            AssignedTo = ToNullableViewModel(wi.Fields.AssignedTo),
            CreatedBy = ToViewModel(wi.Fields.CreatedBy),
            CreatedDate = wi.Fields.SystemCreatedDate,
            IterationPath = wi.Fields.IterationPath ?? string.Empty,
            Type = WorkItemType.FromApiValue(wi.Fields.WorkItemType),
            State = wi.Fields.State,
            ProjectCode = wi.Fields.ProjectCode ?? string.Empty,
            Url = ConnectionSettings.Url
                .AppendPathSegment("_workItems/edit")
                .AppendPathSegment(id),
        };

        return workItem;
    }

    private PullRequest ToViewModel(PullRequestDto pr)
    {
        var reviews = pr.Reviewers
            .Select(ToViewModel)
            .OrderByDescending(x => x.Vote)
            .ThenBy(x => x.Reviewer.DisplayName)
            .ToList();

        var projectName = pr.Repository.Project.Name;
        var repositoryName = pr.Repository.Name;
        var id = pr.PullRequestId;
        var pullRequest = new PullRequest
        {
            Id = id,
            Title = pr.Title,
            RepositoryName = repositoryName,
            Project = projectName,
            Status = pr.IsDraft ? Status.Draft : Status.Open,
            AutoComplete = !string.IsNullOrEmpty(pr.CompletionOptions?.MergeCommitMessage),
            CreationDate = pr.CreationDate,
            CreatedBy = ToViewModel(pr.CreatedBy),
            Reviews = reviews,
            Labels = pr.Labels?.Where(label => label.Active).Select(label => label.Name).ToList() ?? [],
            WorkItems = [],
            Url = ConnectionSettings.Url
                .AppendPathSegment(projectName)
                .AppendPathSegment("_git")
                .AppendPathSegment(repositoryName)
                .AppendPathSegment("pullRequest")
                .AppendPathSegment(id),
        };

        return pullRequest;
    }

    private static Review ToViewModel(Reviewer reviewer)
    {
        return new Review()
        {
            IsRequired = reviewer.IsRequired,
            Vote = (ReviewVote)reviewer.Vote,
            Reviewer = new Person()
            {
                Id = reviewer.Id,
                DisplayName = reviewer.DisplayName,
                AvatarUrl = reviewer.ImageUrl,
            },
        };
    }

    private static Person? ToNullableViewModel(User? user) => user == null ? null : ToViewModel(user);

    private static Person ToViewModel(User user)
    {
        return new Person()
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            AvatarUrl = user.ImageUrl,
        };
    }

}
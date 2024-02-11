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

public class PullRequestService
{
    private IAzureDevOpsServer AzureDevOpsServer { get; }
    private ConnectionSettings ConnectionSettings => AzureDevOpsServer.ConnectionSettings;

    public PullRequestService(IAzureDevOpsServer azureDevOpsServer)
    {
        AzureDevOpsServer = azureDevOpsServer;
    }
    public async Task<IEnumerable<PullRequest>> GetPullRequestsAsync()
    {
        var srv = AzureDevOpsServer;
        var pullRequests = await srv.GetPullRequestsAsync();

        var workItemMap = new Dictionary<int, List<int>>();
        foreach (var pr in pullRequests)
        {
            var idMap = await srv.GetPullRequestWorkItemIdsAsync(pr);
            workItemMap.Add(pr.PullRequestId, idMap.Select(x => x.Id).ToList());
        }

        var workItemIds = workItemMap.SelectMany(kvp => kvp.Value).Distinct();
        var workItems = (await srv.GetWorkItemsAsync(workItemIds))
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
        var workItem = new WorkItem()
        {
            Id = wi.Id,
            Title = wi.Fields.Title,
            AssignedTo = ToNullableViewModel(wi.Fields.AssignedTo),
            CreatedBy = ToViewModel(wi.Fields.CreatedBy),
            CreatedDate = wi.Fields.SystemCreatedDate,
            IterationPath = wi.Fields.IterationPath ?? string.Empty,
            Type = WorkItemType.FromApiValue(wi.Fields.WorkItemType),
            State = wi.Fields.State,
            ProjectCode = wi.Fields.ProjectCode ?? string.Empty,
        };

        workItem.Url = ConnectionSettings.Url
            .AppendPathSegment("_workItems/edit")
            .AppendPathSegment(workItem.Id);

        return workItem;
    }

    private PullRequest ToViewModel(PullRequestDto pr)
    {
        var reviews = pr.Reviewers
            .Select(ToViewModel)
            .OrderByDescending(x => x.Vote)
            .ThenBy(x => x.Reviewer.DisplayName)
            .ToList();

        var pullRequest = new PullRequest
        {
            Id = pr.PullRequestId,
            Title = pr.Title,
            RepositoryName = pr.Repository.Name,
            Project = pr.Repository.Project.Name,
            Status = pr.IsDraft ? Status.Draft : Status.Open,
            AutoComplete = !string.IsNullOrEmpty(pr.CompletionOptions?.MergeCommitMessage),
            CreationDate = pr.CreationDate,
            CreatedBy = ToViewModel(pr.CreatedBy),
            Reviews = reviews,
            Labels = pr.Labels?.Where(label => label.Active).Select(label => label.Name).ToList() ?? new List<string>(),
        };

        pullRequest.Url = ConnectionSettings.Url
            .AppendPathSegment(pullRequest.Project)
            .AppendPathSegment("_git")
            .AppendPathSegment(pullRequest.RepositoryName)
            .AppendPathSegment("pullRequest")
            .AppendPathSegment(pullRequest.Id);

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
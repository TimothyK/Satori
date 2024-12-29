using System.Diagnostics.CodeAnalysis;
using Flurl;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps.Models;
using WorkItem = Satori.AppServices.ViewModels.WorkItems.WorkItem;

namespace Satori.AppServices.Services.Converters
{
    internal static class WorkItemExtensions
    {
        public static WorkItem ToViewModel(this AzureDevOps.Models.WorkItem wi)
        {
            ArgumentNullException.ThrowIfNull(wi);

            try
            {
                return ToViewModelUnsafe(wi);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to build view model for work item {wi.Id}.  {ex.Message}", ex);
            }
        }

        private static WorkItem ToViewModelUnsafe(this AzureDevOps.Models.WorkItem wi)
        {
            var id = wi.Id;
            var workItem = new WorkItem()
            {
                Id = id,
                Title = wi.Fields.Title,
                ProjectName = wi.Fields.ProjectName,
                AssignedTo = wi.Fields.AssignedTo,
                CreatedBy = wi.Fields.CreatedBy,
                CreatedDate = wi.Fields.SystemCreatedDate,
                AreaPath = wi.Fields.AreaPath,
                IterationPath = wi.Fields.IterationPath ?? string.Empty,
                AbsolutePriority = wi.Fields.BacklogPriority > 0.0 ? wi.Fields.BacklogPriority : double.MaxValue,
                Type = WorkItemType.FromApiValue(wi.Fields.WorkItemType),
                State = ScrumState.FromApiValue(wi.Fields.State),
                Triage = TriageState.FromApiValue(wi.Fields.Triage),
                Blocked = wi.Fields.Blocked,
                Tags = ParseTags(wi.Fields.Tags),
                OriginalEstimate = wi.Fields.OriginalEstimate.HoursToTimeSpan(),
                CompletedWork = wi.Fields.CompletedWork.HoursToTimeSpan(),
                RemainingWork = wi.Fields.RemainingWork.HoursToTimeSpan(),
                ProjectCode = wi.Fields.ProjectCode ?? string.Empty,
                Parent = CreateWorkItemPlaceholder(wi.Fields.Parent, UriParser.GetAzureDevOpsOrgUrl(wi.Url)),
                Url = UriParser.GetAzureDevOpsOrgUrl(wi.Url)
                    .AppendPathSegment("_workItems/edit")
                    .AppendPathSegment(id),
                ApiUrl = wi.Url,
                Children = GetChildren(wi.Relations),
            };

            return workItem;
        }

        private static List<WorkItem> GetChildren(List<WorkItemRelation> relations)
        {
            return relations
                .Where(r => r.RelationType == "System.LinkTypes.Hierarchy-Forward")
                .Select(r => CreateWorkItemPlaceholder(int.Parse(r.Url.Split('/').Last()), UriParser.GetAzureDevOpsOrgUrl(r.Url)))
                .ToList();
        }

        [return: NotNullIfNotNull(nameof(workItemId))]
        private static WorkItem? CreateWorkItemPlaceholder(int? workItemId, Uri azureDevOpsOrgUrl)
        {
            if (workItemId == null)
            {
                return null;
            }

            return new WorkItem
            {
                Id = workItemId.Value,
                Title = $"Work Item {workItemId}",
                ProjectName = "TeamProject",
                Url = azureDevOpsOrgUrl
                    .AppendPathSegment("_workItems/edit")
                    .AppendPathSegment(workItemId.Value),
                ApiUrl = azureDevOpsOrgUrl
                    .AppendPathSegment("_apis/wit/workItems")
                    .AppendPathSegment(workItemId),
                AssignedTo = Person.Empty,
                CreatedBy = Person.Empty,
                Type = WorkItemType.Unknown,
                State = ScrumState.Open,
                Tags = [],
            };
        }

        private static List<string> ParseTags(string? tagsString)
        {
            if (string.IsNullOrWhiteSpace(tagsString))
            {
                return [];
            }

            return tagsString.Split(";")
                .Select(tag => tag.Replace("_", " ").Trim())
                .ToList();
        }

        private static TimeSpan? HoursToTimeSpan(this double? value)
        {
            return value == null ? null : TimeSpan.FromHours(value.Value);
        }
    }
}

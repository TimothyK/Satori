using Flurl;
using Satori.AppServices.ViewModels.WorkItems;

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
                AssignedTo = wi.Fields.AssignedTo.ToNullableViewModel(),
                CreatedBy = wi.Fields.CreatedBy.ToViewModel(),
                CreatedDate = wi.Fields.SystemCreatedDate,
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
                Url = UriParser.GetAzureDevOpsOrgUrl(wi.Url)
                    .AppendPathSegment("_workItems/edit")
                    .AppendPathSegment(id),
            };

            return workItem;
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

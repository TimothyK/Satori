﻿using Flurl;
using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.AppServices.Services.Converters
{
    internal static class WorkItemExtensions
    {
        public static WorkItem ToViewModel(this AzureDevOps.Models.WorkItem wi)
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
                AbsolutePriority = wi.Fields.Priority,
                Type = WorkItemType.FromApiValue(wi.Fields.WorkItemType),
                State = wi.Fields.State,
                ProjectCode = wi.Fields.ProjectCode ?? string.Empty,
                Url = UriParser.GetAzureDevOpsOrgUrl(wi.Url)
                    .AppendPathSegment("_workItems/edit")
                    .AppendPathSegment(id),
            };

            return workItem;
        }

    }
}

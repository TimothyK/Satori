using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.AppServices.ViewModels.WorkItems;

namespace Satori.Pages
{
    public partial class EditWorkItem
    {
    }
    public class WorkItemCommentViewModel(WorkItem? workItem, TimeEntry[] timeEntries)
        : CommentViewModel(
            CommentType.WorkItem
            , CommentType.WorkItem.GetComment(timeEntries.FirstOrDefault(entry => entry.Task == workItem))
            , timeEntries
            , timeEntries.Where(entry => entry.Task == workItem)
        )
    {
        public WorkItem? WorkItem { get; set; } = workItem;
    }

}

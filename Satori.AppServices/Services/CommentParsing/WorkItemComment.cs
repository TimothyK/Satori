using System.Text.RegularExpressions;

namespace Satori.AppServices.Services.CommentParsing;

public partial class WorkItemComment : Comment
{
    [GeneratedRegex(@"^D#?(?'id'\d+)[\s-]*(?'title'.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex WorkItemCommentRegex();
    [GeneratedRegex(@"^D#?(?'parentId'\d+)[\s-]*(?'parentTitle'.*)\s+D#?(?'id'\d+)[\s-]*(?'title'.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ParentedWorkItemCommentRegex();

    public new static WorkItemComment? FromVisitor(string text)
    {
        List<(int Id, string Title)> workItems = [];
        var parentedWorkItemRegex = ParentedWorkItemCommentRegex();
        var match =  parentedWorkItemRegex.Match(text);
        if (match.Success)
        {
            var parentId = int.Parse(match.Groups["parentId"].Value);
            var parentTitle = match.Groups["parentTitle"].Value;
            workItems.Add((parentId, parentTitle));
        }
        else
        {
            var workItemRegex = WorkItemCommentRegex();
            match = workItemRegex.Match(text);
            if (!match.Success)
            {
                return null;
            }
        }
            
        var id = int.Parse(match.Groups["id"].Value);
        var title = match.Groups["title"].Value;
        workItems.Add((id, title));
            
        return new WorkItemComment(text, workItems);
    }

    public IReadOnlyCollection<(int Id, string Title)> WorkItems { get; }

    private WorkItemComment(string text, IReadOnlyCollection<(int Id, string Title)> workItems) : base(CommentType.WorkItem, text)
    {
        WorkItems = workItems;
    }
}
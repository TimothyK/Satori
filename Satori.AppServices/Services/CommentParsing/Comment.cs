using CodeMonkeyProjectiles.Linq;

namespace Satori.AppServices.Services.CommentParsing;

public class Comment
{
    public static Comment FromVisitor(string text)
    {
        foreach (var type in CommentType.ScrumTypes)
        {
            if (text.StartsWith(type.Icon))
            {
                return new Comment(type, text[type.Icon.Length..].Trim());
            }
        }

        return new Comment(CommentType.Other, text);
    }

    protected Comment(CommentType type, string text)
    {
        Type = type;
        Text = text;
    }

    public CommentType Type { get; }
    public string Text { get; }
}

public static class CommentExtensions
{
    public static string? Join(this IEnumerable<Comment> comments, Func<CommentType, bool> predicate) => 
        RejoinLines(comments.Where(c => predicate(c.Type)).Select(c => c.Text).ToArray());

    private static string? RejoinLines(string[] lines)
    {
        return lines.None() ? null : string.Join(Environment.NewLine, lines);
    }
}
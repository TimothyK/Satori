using CodeMonkeyProjectiles.Linq;

namespace Satori.AppServices.Services.CommentParsing;

internal static class CommentParser
{
    /// <summary>
    /// Parses the lines in the Kimai time entry Description field and classifies each line into its comment type.
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>
    /// This implements the Visitor design pattern.  This method traverses the lines calling a Visitor method on each comment type.
    /// If format of the line matches the Comment Type, the Visitor returns a Comment object.
    /// If it doesn't match null is returned and the algorithm keeps searching for an appropriate Comment Type.
    /// <see cref="Comment.FromVisitor"/> will always return a value of type <see cref="CommentType.Other"/> if no other type is found.
    /// </para>
    /// </remarks>
    public static IEnumerable<Comment> Parse(string? description)
    {
        var lines = description?.Split('\n')
            .SelectWhereHasValue(x => string.IsNullOrWhiteSpace(x) ? null : x.Trim())
            .Distinct()
            .ToList() ?? [];

        foreach (var line in lines)
        {
            yield return WorkItemComment.FromVisitor(line) ?? Comment.FromVisitor(line);
        }
    }

}
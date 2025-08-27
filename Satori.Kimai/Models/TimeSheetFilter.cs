using Flurl;

namespace Satori.Kimai.Models;

public class TimeSheetFilter
{
    /// <summary>
    /// Inclusive filter for the lower limit on the Begin time field on the time entry.  This filter will be applied as a local time.
    /// </summary>
    public DateTime? Begin { get; set; }
    /// <summary>
    /// Inclusive filter for the upper limit on the Begin time field on the time entry.  This filter will be applied as a local time.
    /// </summary>
    public DateTime? End { get; set; }

    /// <summary>
    /// Set to True to get the time entry record currently being timed.  False to get time entries that have stopped.  Null for both.
    /// </summary>
    public bool? IsRunning { get; set; }

    /// <summary>
    /// Returns time entries for all users in Kimai.  By default, only the current user is returned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This does require in Kimai that the user has access to see other user's time sheets.
    /// Otherwise, it will jus the current user's time sheet.
    /// </para>
    /// </remarks>
    public bool AllUsers { get; set; }

    /// <summary>
    /// Free text search on the description (e.g "D#12345")
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// For pagination, the page number to return
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of Time Entries to return (max per page)
    /// </summary>
    public int Size { get; set; } = 50;
}

public static class TimeSheetFilterExtensions
{
    public static Url AppendQueryParams(this Url url, TimeSheetFilter filter)
    {
        var result = url
            .AppendQueryParam("begin", filter.Begin?.ToString("s"))
            .AppendQueryParam("end", filter.End?.ToString("s"))
            .AppendQueryParam("active", BoolParameter(filter.IsRunning))
            .AppendQueryParam("term", filter.Term)
            .AppendQueryParam("page", filter.Page == 1 ? null : filter.Page)
            .AppendQueryParam("size", filter.Size == 50 ? null : filter.Size);

        if (filter.AllUsers)
        {
            result.AppendQueryParam("user", "all");
        }

        return url;

        static int? BoolParameter(bool? value)
        {
            return value == null ? null 
                : value.Value ? 1 
                : 0;
        }
    }
    
}
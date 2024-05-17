using Moq;
using Satori.Kimai;
using Satori.Kimai.Models;
using Shouldly;
using System.Net;

namespace Satori.AppServices.Tests.TestDoubles.Kimai;

internal class TestKimaiServer
{
    private readonly Mock<IKimaiServer> _mock;

    public TestKimaiServer()
    {
        _mock = new Mock<IKimaiServer>(MockBehavior.Strict);
        _mock.Setup(srv => srv.GetTimeSheetAsync(It.IsAny<TimeSheetFilter>()))
            .ReturnsAsync((TimeSheetFilter filter) => GetTimeSheet(filter));
    }

    public IKimaiServer AsInterface() => _mock.Object;

    private List<TimeEntry> TimeSheet { get; } = [];

    public void AddTimeEntry(TimeEntry entry)
    {
        TimeSheet.Add(entry);
    }

    private TimeEntry[] GetTimeSheet(TimeSheetFilter filter)
    {
        if (ExpectedPageSize != null)
        {
            filter.Size.ShouldBe(ExpectedPageSize.Value);
        }

        var entries = TimeSheet
            .Where(x => filter.Begin == null || filter.Begin <= x.Begin)
            .Where(x => filter.End == null || x.Begin <= filter.End)
            .Where(x => filter.IsRunning == null || (filter.IsRunning.Value && x.End == null) || (!filter.IsRunning.Value && x.End != null))
            .Where(x => filter.Term == null || (x.Description?.Contains(filter.Term) ?? false))
            .OrderByDescending(x => x.Begin)
            .Skip((filter.Page-1) * filter.Size)
            .Take(filter.Size)
            .ToArray();

        if (filter.Page > 1 && entries.Length == 0)
        {
            throw new HttpRequestException("Bad Response: 404 - Not Found", inner: null, HttpStatusCode.NotFound);
        }
        
        return entries.ToArray();
    }

    public int? ExpectedPageSize { get; set; }

    public TimeEntry? GetLastEntry(DateOnly? day)
    {
        var filter = new TimeSheetFilter() {Size = 1};
        if (day != null)
        {
            filter.Begin = day.Value.ToDateTime(TimeOnly.MinValue);
            filter.End = day.Value.ToDateTime(TimeOnly.MaxValue);
        }

        return GetTimeSheet(filter).FirstOrDefault();
    }

}
using Moq;
using Satori.Kimai;
using Satori.Kimai.Models;
using Shouldly;
using System.Net;
using Builder;
using Satori.AppServices.Extensions;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Services;
using Satori.TimeServices;

namespace Satori.AppServices.Tests.TestDoubles.Kimai;

internal class TestKimaiServer
{
    public readonly Mock<IKimaiServer> Mock;


    public TestKimaiServer()
    {
        Mock = new Mock<IKimaiServer>(MockBehavior.Strict);

        Mock.Setup(srv => srv.Enabled)
            .Returns(() => Enabled);

        Mock.Setup(srv => srv.GetTimeSheetAsync(It.IsAny<TimeSheetFilter>()))
            .ReturnsAsync((TimeSheetFilter filter) => GetTimeSheet(filter));

        Mock.Setup(srv => srv.GetTimeEntryAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => GetTimeEntry(id));

        Mock.Setup(srv => srv.BaseUrl)
            .Returns(BaseUrl);

        Mock.Setup(srv => srv.GetMyUserAsync())
            .ReturnsAsync(() => CurrentUser!);

        Mock.Setup(srv => srv.ExportTimeSheetAsync(It.IsAny<int>()))
            .Callback((int id) => MarkAsExported(id))
            .Returns(Task.CompletedTask);

        Mock.Setup(srv => srv.StopTimerAsync(It.IsAny<int>()))
            .Callback((int id) => StopTimer(id))
            .ReturnsAsync((int id) => TimeSheet.Single(t => t.Id == id).End ?? throw new ApplicationException($"{nameof(StopTimer)} did not set End value"));

        Mock.Setup(srv => srv.UpdateTimeEntryDescriptionAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Callback((int id, string description) => UpdateDescription(id, description))
            .Returns(Task.CompletedTask);

        Mock.Setup(srv => srv.CreateTimeEntryAsync(It.IsAny<TimeEntryForCreate>()))
            .ReturnsAsync((TimeEntryForCreate entry) => CreateTimeEntry(entry));

        CurrentUser = KimaiUserBuilder.BuildUser();
    }

    public bool Enabled { get; set; } = true;

    public Uri BaseUrl { get; } = new("https://kimai.test/");

    public User CurrentUser { get; }

    public IKimaiServer AsInterface() => Mock.Object;

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
            .Where(x => x.User == CurrentUser) // Kimai will filter by current user by default.  Our Kimai implementation doesn't support user filtering yet, so the Kimai behaviour will be to filter on current user.
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

    public TimeEntry? GetLastEntry(DateOnly? day = null)
    {
        var filter = new TimeSheetFilter() {Size = 1};
        if (day != null)
        {
            filter.Begin = day.Value.ToDateTime(TimeOnly.MinValue);
            filter.End = day.Value.ToDateTime(TimeOnly.MaxValue);
        }
        
        return GetTimeSheet(filter).FirstOrDefault();
    }

    private void MarkAsExported(int id)
    {
        var entry = TimeSheet.SingleOrDefault(x => x.Id == id)
                    ?? throw new InvalidOperationException($"Id {id} not found");

        if (entry.Exported)
        {
            throw new InvalidOperationException($"Id {id} already exported");
        }

        entry.Exported = true;
    }

    public ITimeServer TimeServer { get; set; } = new TestTimeServer();

    private void StopTimer(int id)
    {
        var entry = TimeSheet.SingleOrDefault(x => x.Id == id)
                    ?? throw new InvalidOperationException($"Id {id} not found");

        if (entry.End.HasValue)
        {
            throw new InvalidOperationException($"Id {id} already stopped");
        }

        var end = TimeServer.GetUtcNow().ToNearest(TimeSpan.FromMinutes(1), RoundingDirection.Floor);
        if (end < entry.Begin)
        {
            end = entry.Begin + TimeSpan.FromMinutes(3);
        }
        entry.End = end;
    }

    private void UpdateDescription(int id, string description)
    {
        var entry = TimeSheet.SingleOrDefault(x => x.Id == id)
                    ?? throw new InvalidOperationException($"Id {id} not found");

        entry.Description = description;
    }

    private TimeEntryCollapsed GetTimeEntry(int id)
    {
        var entry = TimeSheet.SingleOrDefault(x => x.Id == id)
                    ?? throw new HttpRequestException("Not Found", null, HttpStatusCode.NotFound);

        return new TimeEntryCollapsed()
        {
            Activity = entry.Activity.Id,
            Id = entry.Id,
            Begin = entry.Begin,
            End = entry.End,
            Project = entry.Project.Id,
            User = entry.User.Id,
            Description = entry.Description,
            Exported = entry.Exported,
        };
    }

    #region CreateTimeEntry

    private TimeEntry CreateTimeEntry(TimeEntryForCreate payload)
    {
        var activity = FindOrCreateActivity(payload.Activity, payload.Project);
        var entry = new TimeEntry
        {
            Id = Sequence.TimeEntryId.Next(),
            User = FindOrCreateUser(payload.User),
            Activity = activity,
            Project = activity.Project,
            Begin = payload.Begin,
            End = payload.End,
            Description = payload.Description,
            Exported = payload.Exported,
        };
        
        AddTimeEntry(entry);

        return entry;
    }

    private Activity FindOrCreateActivity(int activityId, int projectId)
    {
        return TimeSheet.Select(t => t.Activity).FirstOrDefault(activity => activity.Id == activityId) 
               ?? Builder<Activity>.New().Build(activity =>
               {
                   activity.Id = activityId;
                   activity.Project = FindOrCreateProject(projectId);
                   activity.Visible = true;
               });
    }

    private Project FindOrCreateProject(int projectId)
    {
        return TimeSheet.Select(t => t.Project).FirstOrDefault(project => project.Id == projectId) 
               ?? Builder<Project>.New().Build(project =>
               {
                   project.Id = projectId;
                   project.Customer = Builder<Customer>.New().Build(customer =>
                   {
                       customer.Id = Sequence.CustomerId.Next();
                       customer.Name = $"Customer {customer.Id}";
                       customer.Number = $"FSK-{customer.Id.ToString().PadLeft(4, '0')}";
                       customer.Visible = true;
                   });
                   project.Visible = true;
                   project.Name = $"{projectId.ToString().PadLeft(4, '0')} Project";
               });
    }


    private User FindOrCreateUser(int userId)
    {
        if (CurrentUser.Id == userId)
        {
            return CurrentUser;
        }

        return TimeSheet.Select(t => t.User).FirstOrDefault(user => user.Id == userId) 
               ?? Builder<User>.New().Build(user =>
                {
                    user.Id = userId;
                    user.Enabled = true;
                    user.Language = "en_CA";
                });
    }

    #endregion
}
using Builder;
using Microsoft.Extensions.Logging.Abstractions;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.Tests.TestDoubles.MessageQueues;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai.Models;
using KimaiTimeEntry = Satori.Kimai.Models.TimeEntry;

namespace Satori.AppServices.Tests.DailyStandUps;

public abstract class DailyStandUpTests
{
    protected StandUpService Server { get; }

    protected DailyStandUpTests()
    {
        Person.Me = null;  //Clear cache

        var alertService = new AlertService();
        var userService = new UserService(AzureDevOps.AsInterface(), Kimai.AsInterface(), alertService);
        Server = new StandUpService(
            Kimai.AsInterface()
            , AzureDevOps.AsInterface()
            , userService
            , DailyActivityExporter
            , TaskAdjustmentExporter
            , NullLoggerFactory.Instance
            , alertService);
    }

    #region Helpers

    #region Arrange

    private protected TestAzureDevOpsServer AzureDevOps { get; } = new();

    private protected TestKimaiServer Kimai { get; } = new() {CurrentUser = DefaultUser};

    private protected TestTaskAdjustmentExporter TaskAdjustmentExporter { get; } = new();
    private protected TestDailyActivityExporter DailyActivityExporter { get; } = new();

    protected static readonly User DefaultUser = Builder<User>.New().Build(user =>
    {
        user.Id = Sequence.KimaiUserId.Next();
        user.Enabled = true;
        user.Language = "en_CA";
    });

    protected Activity[] TestActivities { get; } = BuildActivities();

    private static Activity[] BuildActivities()
    {
        var customers = Enumerable.Range(1, 3).Select(_ => Sequence.CustomerId.Next())
            .Select(id => new Customer()
                {
                    Id = id,
                    Name = $"Customer {id}",
                    Number = $"FSK-{id.ToString().PadLeft(4, '0')}",
                    Visible = true,
                }
            ).ToArray();
        var projects = customers.SelectMany(customer => Enumerable.Range(1, 3).Select(_ => Sequence.ProjectId.Next())
            .Select(id => new Project()
                {
                    Id = id,
                    Name = $"Project {id}",
                    Customer = customer,
                    Visible = true,
                    GlobalActivities = true,
                }
            )
        ).ToArray();

        var activities = projects.SelectMany(project => Enumerable.Range(1, 3).Select(_ => Sequence.ActivityId.Next())
            .Select(id => new Activity()
                {
                    Id = id,
                    Name = $"Activity {id}",
                    Project = project,
                    Visible = true,
                    Comment = $"Test activity comment [Fsk={id}]",
                }
            )
        ).ToArray();
        return activities;
    }

    private static DateOnly? _today;
    protected static DateOnly Today => _today ?? (_today = DateOnly.FromDateTime(DateTime.Today)).Value;

    protected KimaiTimeEntry BuildTimeEntry(DateOnly day) => 
        BuildTimeEntry(day, TimeSpan.FromMinutes(30).Randomize());

    protected KimaiTimeEntry BuildTimeEntry(Activity activity) => 
        BuildTimeEntry(activity, Today);

    protected KimaiTimeEntry BuildTimeEntry(DateOnly day, TimeSpan duration) => 
        BuildTimeEntry(TestActivities.SingleRandom(), day, duration);
    
    protected KimaiTimeEntry BuildTimeEntry(Activity activity, DateOnly day) => 
        BuildTimeEntry(activity, day, TimeSpan.FromMinutes(30).Randomize());

    protected KimaiTimeEntry BuildTimeEntry(Activity activity, DateOnly day, TimeSpan duration)
    {
        var lastEntry = Kimai.GetLastEntry(day);
        if (lastEntry != null && lastEntry.End == null)
        {
            lastEntry.End = lastEntry.Begin.Add(duration).TruncateSeconds();
        }
        var defaultStartOfDay = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Local)) //8:00 AM, local time zone
            .Add(TimeSpan.FromMinutes(5).Randomize());  //ish
        var begin = lastEntry?.End ?? defaultStartOfDay;

        var entry = new KimaiTimeEntry()
        {
            Id = Sequence.TimeEntryId.Next(),
            Begin = begin.TruncateSeconds(),
            End = begin.Add(duration).TruncateSeconds(),
            User = DefaultUser,
            Activity = activity,
            Project = activity.Project,
            Exported = false,
        };

        Kimai.AddTimeEntry(entry);
        
        return entry;
    }

    #endregion Arrange

    #region Act

    protected async Task<PeriodSummary> GetPeriodAsync(DateOnly begin, DateOnly end)
    {
        return await Server.GetStandUpPeriodAsync(begin, end);
    }

    #endregion Act

    #endregion Helpers


}
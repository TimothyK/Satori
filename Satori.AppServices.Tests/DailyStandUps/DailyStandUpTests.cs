using Builder;
using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.Extensions;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.DailyStandUps;
using Satori.Kimai.Models;
using Shouldly;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class DailyStandUpTests
{
    #region Helpers

    #region Arrange

    private TestKimaiServer Kimai { get; } = new() {CurrentUser = DefaultUser};

    private static readonly User DefaultUser = Builder<User>.New().Build(user =>
    {
        user.Enabled = true;
        user.Language = "en_CA";
    });

    private Activity[] TestActivities { get; } = BuildActivities();

    private static Activity[] BuildActivities()
    {
        var customers = Enumerable.Range(1, 3)
            .Select(i => new Customer()
                {
                    Id = i,
                    Name = $"Customer {i}",
                    Number = $"FSK-{i.ToString().PadLeft(4, '0')}",
                    Visible = true,
                }
            ).ToArray();
        var projectId = 0;
        var projects = customers.SelectMany(customer => Enumerable.Range(1, 3).Select(_ => ++projectId)
            .Select(i => new Project()
                {
                    Id = i,
                    Name = $"Project {i}",
                    Customer = customer,
                    Visible = true,
                    GlobalActivities = true,
                }
            )
        ).ToArray();

        var activityId = 0;
        var activities = projects.SelectMany(project => Enumerable.Range(1, 3).Select(_ => ++activityId)
            .Select(i => new Activity()
                {
                    Id = i,
                    Name = $"Activity {i}",
                    Project = project,
                    Visible = true,
                    Comment = $"Test activity comment [Fsk={i}]",
                }
            )
        ).ToArray();
        return activities;
    }

    private int _entryId;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private TimeEntry BuildTimeEntry(DateOnly day) => BuildTimeEntry(day, TimeSpan.FromMinutes(30).Randomize());
    private TimeEntry BuildTimeEntry(DateOnly day, TimeSpan duration)
    {
        var lastEntry = Kimai.GetLastEntry(day);
        if (lastEntry != null && lastEntry.End == null)
        {
            lastEntry.End = lastEntry.Begin.Add(duration).TruncateSeconds();
        }
        var defaultStartOfDay = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Local)) //8:00 AM, local time zone
            .Add(TimeSpan.FromMinutes(5).Randomize());  //ish
        var begin = lastEntry?.End ?? defaultStartOfDay;

        var activity = TestActivities.SingleRandom();

        var entry = new TimeEntry()
        {
            Id = Interlocked.Increment(ref _entryId),
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

    private async Task<StandUpDay[]> GetStandUpDaysAsync(DateOnly begin, DateOnly end)
    {
        var srv = new StandUpService(Kimai.AsInterface());

        return await srv.GetStandUpDaysAsync(begin, end);
    }

    #endregion Act

    #endregion Helpers


    [TestMethod]
    public async Task ASmokeTest()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);
        entry.End.ShouldNotBeNull();

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Length.ShouldBe(1);
        var day = days.Single();
        day.Date.ShouldBe(today);
        day.TotalTime.ShouldBe(entry.End.Value - entry.Begin);
    }
    
    [TestMethod]
    public void InvalidDateRange_ThrowsException()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Should.ThrowAsync<ArgumentException>(() => GetStandUpDaysAsync(today, today.AddDays(-1)));
    }

    [TestMethod]
    public async Task RunningTimeEntry_IsNotReported()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);
        entry.End = null;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().TotalTime.ShouldBe(TimeSpan.Zero);
    }
    
    [TestMethod]
    public async Task DifferentDay_IsNotReported()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        BuildTimeEntry(today);

        //Act
        var days = await GetStandUpDaysAsync(yesterday, yesterday);

        //Assert
        days.Length.ShouldBe(1);
        days.Single().Date.ShouldBe(yesterday);
        days.Single().TotalTime.ShouldBe(TimeSpan.Zero);
    }


    #region Page Size

    private const int ExpectedPageSize = 250;

    [TestMethod]
    public void VerifyExpectedPageSize()
    {
        //Arrange
        var today = Today;
        Kimai.ExpectedPageSize = ExpectedPageSize + 1;

        //Act
        Should.ThrowAsync<ShouldAssertException>(() => GetStandUpDaysAsync(today, today));
    }

    [TestMethod]
    public async Task MultiplePages_LoadsAllData()
    {
        //Arrange
        var today = Today;
        const int expectedTime = ExpectedPageSize + 1;
        foreach (var _ in Enumerable.Range(1, expectedTime))
        {
            BuildTimeEntry(today, TimeSpan.FromMinutes(1));
        }
        Kimai.ExpectedPageSize = ExpectedPageSize;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().TotalTime.ShouldBe(TimeSpan.FromMinutes(expectedTime));
    }
    
    /// <summary>
    /// Kimai will throw a 404 if the page is not found.  The TestKimaiServer is implemented to throw this.
    /// This test ensures the service under test can handle this expected exception.
    /// </summary>
    [TestMethod]
    public async Task MultiplePagesWithNotRemainder_LoadsAllData()
    {
        //Arrange
        var today = Today;
        const int expectedTime = ExpectedPageSize * 2;
        foreach (var _ in Enumerable.Range(1, expectedTime))
        {
            BuildTimeEntry(today, TimeSpan.FromMinutes(1));
            
        }
        Kimai.ExpectedPageSize = ExpectedPageSize;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().TotalTime.ShouldBe(TimeSpan.FromMinutes(expectedTime));
    }

    #endregion Page Size

    #region Exported

    [TestMethod]
    public async Task AllExported()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;
        BuildTimeEntry(today).Exported = true;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task AllExported_False()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;
        BuildTimeEntry(today).Exported = false;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_AlreadyExported_False()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_ReadyToExport_True()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = false;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().CanExport.ShouldBeTrue();
    }
    
    [TestMethod]
    public async Task CanExport_ActivityDeactivated_False()
    {
        //Arrange
        var today = Today;
        var activity = TestActivities.Last().Copy().With(x => x.Id = TestActivities.GetNextId(seq => seq.Id));
        activity.Visible = false;
        BuildTimeEntry(today).With(entry =>
        {
            entry.Exported = false;
            entry.Activity = activity;
        });

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_ActivityTbd_False()
    {
        //Arrange
        var today = Today;
        var activity = TestActivities.Last().Copy().With(x => x.Id = TestActivities.GetNextId(seq => seq.Id));
        activity.Name = "TBD";
        BuildTimeEntry(today).With(entry =>
        {
            entry.Exported = false;
            entry.Activity = activity;
        });

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_ProjectDeactivated_False()
    {
        //Arrange
        var today = Today;
        var project = TestActivities.Last().Project.Copy().With(p => p.Id = TestActivities.GetNextId(seq => seq.Project.Id));
        project.Visible = false;
        var activity = TestActivities.Last().Copy().With(a =>
        {
            a.Id = TestActivities.GetNextId(seq => seq.Id);
            a.Project = project;
        });
        BuildTimeEntry(today).With(entry =>
        {
            entry.Exported = false;
            entry.Activity = activity;
            entry.Project = project;
        });

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_ProjectTbd_False()
    {
        //Arrange
        var today = Today;
        var project = TestActivities.Last().Project.Copy().With(p => p.Id = TestActivities.GetNextId(seq => seq.Project.Id));
        project.Name = "TBD";
        var activity = TestActivities.Last().Copy().With(a =>
        {
            a.Id = TestActivities.GetNextId(seq => seq.Id);
            a.Project = project;
        });
        BuildTimeEntry(today).With(entry =>
        {
            entry.Exported = false;
            entry.Activity = activity;
            entry.Project = project;
        });

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public async Task CanExport_CustomerDeactivated_False()
    {
        //Arrange
        var today = Today;
        var customer = TestActivities.Last().Project.Customer.Copy().With(cust => cust.Id = TestActivities.GetNextId(seq => seq.Project.Customer.Id));
        customer.Visible = false;
        var project = TestActivities.Last().Project.Copy().With(p =>
        {
            p.Id = TestActivities.GetNextId(seq => seq.Project.Id);
            p.Customer = customer;
        });
        var activity = TestActivities.Last().Copy().With(a =>
        {
            a.Id = TestActivities.GetNextId(seq => seq.Id);
            a.Project = project;
        });
        BuildTimeEntry(today).With(entry =>
        {
            entry.Exported = false;
            entry.Activity = activity;
            entry.Project = project;
        });

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }

    #endregion Exported

    [TestMethod]
    public async Task Url()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today);

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Length.ShouldBe(1);
        var day = days.Single();
        var expected = Kimai.BaseUrl
                .AppendPathSegments(DefaultUser.Language, "timesheet")
                .AppendQueryParam("daterange", $"{today:O} - {today:O}")
                .AppendQueryParam("state", 3)  // stopped
                .AppendQueryParam("billable", 0)
                .AppendQueryParam("exported", 1)
                .AppendQueryParam("orderBy", "begin")
                .AppendQueryParam("order", "DESC")
                .AppendQueryParam("searchTerm", string.Empty)
                .AppendQueryParam("performSearch", "performSearch")
                .ToUri();
        day.Url.ShouldBe(expected);
    }

    [TestMethod]
    public async Task MultipleDays()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        BuildTimeEntry(yesterday, TimeSpan.FromMinutes(10));
        BuildTimeEntry(today, TimeSpan.FromMinutes(15));
        BuildTimeEntry(today, TimeSpan.FromMinutes(20));

        //Act
        var days = await GetStandUpDaysAsync(yesterday, today);

        //Assert
        days.Length.ShouldBe(2);
        days.Single(day => day.Date == yesterday).TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
    }

    [TestMethod]
    public async Task ReturnEmptyDays()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        var yesterdayEve = today.AddDays(-2);
        BuildTimeEntry(yesterdayEve, TimeSpan.FromMinutes(10));
        BuildTimeEntry(today, TimeSpan.FromMinutes(20));

        //Act
        var days = await GetStandUpDaysAsync(yesterdayEve, today);

        //Assert
        days.Length.ShouldBe(3);
        days.Single(day => day.Date == yesterdayEve).TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        days.Single(day => day.Date == yesterday).TotalTime.ShouldBe(TimeSpan.Zero);
        days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(20));
    }

    [TestMethod]
    public async Task DaysAreOrderedDescending()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        var yesterdayEve = today.AddDays(-2);
        BuildTimeEntry(yesterdayEve, TimeSpan.FromMinutes(10));
        BuildTimeEntry(today, TimeSpan.FromMinutes(20));

        //Act
        var days = await GetStandUpDaysAsync(yesterdayEve, today);

        //Assert
        days.Length.ShouldBe(3);
        days[0].Date.ShouldBe(today);
        days[1].Date.ShouldBe(yesterday);
        days[2].Date.ShouldBe(yesterdayEve);
    }

    [TestMethod]
    public void LimitDateRange()
    {
        var today = Today;
        Should.ThrowAsync<ArgumentException>(() => GetStandUpDaysAsync(today.AddDays(-7), today));
    }
    
    [TestMethod]
    public void AllowMaxOneWeek()
    {
        var today = Today;
        Should.NotThrowAsync(() => GetStandUpDaysAsync(today.AddDays(-6), today));
    }

    [TestMethod]
    public async Task FutureDays_DoNotReported()
    {
        //Arrange
        var today = Today;
        var tomorrow = today.AddDays(1);
        BuildTimeEntry(today, TimeSpan.FromMinutes(20));
        BuildTimeEntry(tomorrow, TimeSpan.FromMinutes(10));

        //Act
        var days = await GetStandUpDaysAsync(today, tomorrow);

        //Assert
        days.Length.ShouldBe(1);
        days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(20));
    }
}
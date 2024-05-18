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

    private StandUpDay[] GetStandUpDays(DateOnly begin, DateOnly end)
    {
        var srv = new StandUpService(Kimai.AsInterface());

        return srv.GetStandUpDaysAsync(begin, end).Result;
    }

    #endregion Act

    #endregion Helpers


    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);
        entry.End.ShouldNotBeNull();

        //Act
        var days = GetStandUpDays(today, today);

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
        Should.Throw<AggregateException>(() => GetStandUpDays(today, today.AddDays(-1)))
            .InnerExceptions.OfType<ArgumentException>().ShouldNotBeEmpty();
    }

    [TestMethod]
    public void RunningTimeEntry_IsNotReported()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);
        entry.End = null;

        //Act
        var days = GetStandUpDays(today, today);

        //Assert
        days.ShouldBeEmpty();
    }
    
    [TestMethod]
    public void DifferentDay_IsNotReported()
    {
        //Arrange
        var yesterday = Today.AddDays(-1);
        BuildTimeEntry(yesterday.AddDays(1));

        //Act
        var days = GetStandUpDays(yesterday, yesterday);

        //Assert
        days.ShouldBeEmpty();
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
        Should.Throw<AggregateException>(() => GetStandUpDays(today, today))
            .InnerExceptions.OfType<ShouldAssertException>().ShouldNotBeEmpty();
    }

    [TestMethod]
    public void MultiplePages_LoadsAllData()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().TotalTime.ShouldBe(TimeSpan.FromMinutes(expectedTime));
    }
    
    /// <summary>
    /// Kimai will throw a 404 if the page is not found.  The TestKimaiServer is implemented to throw this.
    /// This test ensures the service under test can handle this expected exception.
    /// </summary>
    [TestMethod]
    public void MultiplePagesWithNotRemainder_LoadsAllData()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().TotalTime.ShouldBe(TimeSpan.FromMinutes(expectedTime));
    }

    #endregion Page Size

    [TestMethod]
    public void MultipleDays()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        BuildTimeEntry(yesterday, TimeSpan.FromMinutes(10));
        BuildTimeEntry(today, TimeSpan.FromMinutes(15));
        BuildTimeEntry(today, TimeSpan.FromMinutes(20));

        //Act
        var days = GetStandUpDays(yesterday, today);

        //Assert
        days.Length.ShouldBe(2);
        days.Single(day => day.Date == yesterday).TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
    }

    #region Exported

    [TestMethod]
    public void AllExported()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;
        BuildTimeEntry(today).Exported = true;

        //Act
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeTrue();
    }
    
    [TestMethod]
    public void AllExported_False()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;
        BuildTimeEntry(today).Exported = false;

        //Act
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
    }
    
    [TestMethod]
    public void CanExport_AlreadyExported_False()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;

        //Act
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public void CanExport_ReadyToExport_True()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = false;

        //Act
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().CanExport.ShouldBeTrue();
    }
    
    [TestMethod]
    public void CanExport_ActivityDeactivated_False()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public void CanExport_ActivityTbd_False()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public void CanExport_ProjectDeactivated_False()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public void CanExport_ProjectTbd_False()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }
    
    [TestMethod]
    public void CanExport_CustomerDeactivated_False()
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
        var days = GetStandUpDays(today, today);

        //Assert
        days.Single().AllExported.ShouldBeFalse();
        days.Single().CanExport.ShouldBeFalse();
    }

    #endregion Exported

    [TestMethod]
    public void Url()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today);

        //Act
        var days = GetStandUpDays(today, today);

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
}
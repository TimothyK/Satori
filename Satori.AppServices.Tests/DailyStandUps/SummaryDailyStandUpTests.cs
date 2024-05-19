using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Tests.Extensions;
using Satori.AppServices.Tests.TestDoubles;
using Shouldly;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class SummaryDailyStandUpTests : DailyStandUpTests
{
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
        var activity = TestActivities.Last().Copy().With(x => x.Id = Sequence.ActivityId.Next());
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
        var activity = TestActivities.Last().Copy().With(x => x.Id = Sequence.ActivityId.Next());
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
        var project = TestActivities.Last().Project.Copy().With(p => p.Id = Sequence.ProjectId.Next());
        project.Visible = false;
        var activity = TestActivities.Last().Copy().With(a =>
        {
            a.Id = Sequence.ActivityId.Next();
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
        var project = TestActivities.Last().Project.Copy().With(p => p.Id = Sequence.ProjectId.Next());
        project.Name = "TBD";
        var activity = TestActivities.Last().Copy().With(a =>
        {
            a.Id = Sequence.ActivityId.Next();
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
        var customer = TestActivities.Last().Project.Customer.Copy().With(cust => cust.Id = Sequence.CustomerId.Next());
        customer.Visible = false;
        var project = TestActivities.Last().Project.Copy().With(p =>
        {
            p.Id = Sequence.ProjectId.Next();
            p.Customer = customer;
        });
        var activity = TestActivities.Last().Copy().With(a =>
        {
            a.Id = Sequence.ActivityId.Next();
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

    #region Multiple Days

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

    #endregion Multiple Days

    #region Projects

    [TestMethod]
    public async Task EmptyTime_ProjectsEmpty()
    {
        //Arrange
        var today = Today;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days[0].Projects.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ProjectIdProperties()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today, TimeSpan.FromMinutes(24));

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days[0].Projects.Length.ShouldBe(1);
        var project = days[0].Projects.Single();
        project.ProjectId.ShouldBe(entry.Project.Id);
        project.ProjectName.ShouldBe(entry.Project.Name);
        project.CustomerId.ShouldBe(entry.Project.Customer.Id);
        project.CustomerName.ShouldBe(entry.Project.Customer.Name);
    }

    [TestMethod]
    public async Task MultipleProjects()
    {
        //Arrange
        var today = Today;
        var project1Activity = TestActivities.SingleRandom();
        var project2Activity1 = TestActivities.Where(a => a.Project.Id != project1Activity.Project.Id).SingleRandom();
        var project2Activity2 = TestActivities.Where(a => a.Project.Id == project2Activity1.Project.Id && a != project2Activity1).SingleRandom();
        BuildTimeEntry(project1Activity, today, TimeSpan.FromMinutes(10));
        BuildTimeEntry(project2Activity1, today, TimeSpan.FromMinutes(20));
        BuildTimeEntry(project2Activity2, today, TimeSpan.FromMinutes(15));

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Length.ShouldBe(1);
        days[0].Projects.Length.ShouldBe(2);
        days[0].Projects[0].ProjectId.ShouldBe(project2Activity1.Project.Id);
        days[0].Projects[0].TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
        days[0].Projects[1].ProjectId.ShouldBe(project1Activity.Project.Id);
        days[0].Projects[1].TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public async Task MultipleProjectsOrdered()
    {
        //Arrange
        var today = Today;
        var project1Activity = TestActivities.SingleRandom();
        var project2Activity1 = TestActivities.Where(a => a.Project.Id != project1Activity.Project.Id).SingleRandom();
        var project2Activity2 = TestActivities.Where(a => a.Project.Id == project2Activity1.Project.Id && a != project2Activity1).SingleRandom();
        BuildTimeEntry(project1Activity, today, TimeSpan.FromMinutes(60));
        BuildTimeEntry(project2Activity1, today, TimeSpan.FromMinutes(20));
        BuildTimeEntry(project2Activity2, today, TimeSpan.FromMinutes(15));

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Length.ShouldBe(1);
        days[0].Projects.Length.ShouldBe(2);
        days[0].Projects[0].ProjectId.ShouldBe(project1Activity.Project.Id);
        days[0].Projects[0].TotalTime.ShouldBe(TimeSpan.FromMinutes(60));
        days[0].Projects[1].ProjectId.ShouldBe(project2Activity1.Project.Id);
        days[0].Projects[1].TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
    }

    [TestMethod]
    public async Task ProjectExported()
    {
        //Arrange
        var today = Today;
        var project1Activity = TestActivities.SingleRandom();
        var project2Activity1 = TestActivities.Where(a => a.Project.Id != project1Activity.Project.Id).SingleRandom();
        var project2Activity2 = TestActivities.Where(a => a.Project.Id == project2Activity1.Project.Id && a != project2Activity1).SingleRandom();
        BuildTimeEntry(project1Activity, today, TimeSpan.FromMinutes(60)).Exported = true;
        BuildTimeEntry(project2Activity1, today, TimeSpan.FromMinutes(20)).Exported = true;
        BuildTimeEntry(project2Activity2, today, TimeSpan.FromMinutes(15)).Exported = false;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Length.ShouldBe(1);
        days[0].Projects.Length.ShouldBe(2);
        days[0].Projects[0].AllExported.ShouldBeTrue();
        days[0].Projects[0].CanExport.ShouldBeFalse();
        days[0].Projects[1].AllExported.ShouldBeFalse();
        days[0].Projects[1].CanExport.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ProjectUrl()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
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
            .AppendQueryParam("projects[]", entry.Project.Id)
            .ToUri();
        days[0].Projects[0].Url.ShouldBe(expected);
    }

    #endregion Projects

    #region Activities

    [TestMethod]
    public async Task MultipleActivities()
    {
        //Arrange
        var today = Today;
        var project = TestActivities.SingleRandom().Project;
        var activities = TestActivities.Where(a => a.Project.Id == project.Id).ToArray();
        BuildTimeEntry(activities[0], today, TimeSpan.FromMinutes(20)).Exported = true;
        BuildTimeEntry(activities[1], today, TimeSpan.FromMinutes(10)).Exported = false;
        BuildTimeEntry(activities[2], today, TimeSpan.FromMinutes(15)).Exported = true;
        BuildTimeEntry(activities[2], today, TimeSpan.FromMinutes(35)).Exported = false;

        //Act
        var days = await GetStandUpDaysAsync(today, today);

        //Assert
        days.Length.ShouldBe(1);
        days[0].Projects[0].Activities.Length.ShouldBe(3);
        days[0].Projects[0].Activities[0].ActivityId.ShouldBe(activities[2].Id);
        days[0].Projects[0].Activities[0].ActivityName.ShouldBe(activities[2].Name);
        days[0].Projects[0].Activities[0].Comment.ShouldBe(activities[2].Comment);
        days[0].Projects[0].Activities[0].TotalTime.ShouldBe(TimeSpan.FromMinutes(50));
        days[0].Projects[0].Activities[0].AllExported.ShouldBeFalse();
        days[0].Projects[0].Activities[0].CanExport.ShouldBeTrue();

        days[0].Projects[0].Activities[1].ActivityId.ShouldBe(activities[0].Id);
        days[0].Projects[0].Activities[1].TotalTime.ShouldBe(TimeSpan.FromMinutes(20));
        days[0].Projects[0].Activities[1].AllExported.ShouldBeTrue();
        days[0].Projects[0].Activities[1].CanExport.ShouldBeFalse();

        days[0].Projects[0].Activities[2].ActivityId.ShouldBe(activities[1].Id);
        days[0].Projects[0].Activities[2].TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        days[0].Projects[0].Activities[2].AllExported.ShouldBeFalse();
        days[0].Projects[0].Activities[2].CanExport.ShouldBeTrue();


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
            .AppendQueryParam("projects[]", activities[1].Project.Id)
            .AppendQueryParam("activities[]", activities[1].Id)
            .ToUri();
        days[0].Projects[0].Activities[2].Url.ShouldBe(expected);
    }

    #endregion Activities

}
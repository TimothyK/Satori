﻿using CodeMonkeyProjectiles.Linq;
using Flurl;
using Satori.AppServices.Tests.Extensions;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.ViewModels.DailyStandUps;
using Shouldly;

namespace Satori.AppServices.Tests.DailyStandUps;

[TestClass]
public class SummaryDailyStandUpTests : DailyStandUpTests
{
    #region Helpers

    #region Act

    private async Task<DaySummary> GetDayAsync(DateOnly day)
    {
        var period = await GetPeriodAsync(day, day);
        period.Days.Count.ShouldBe(1);
        return period.Days.Single();
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
        var day = await GetDayAsync(today);

        //Assert
        day.Date.ShouldBe(today);
        day.TotalTime.ShouldBe(entry.End.Value - entry.Begin);
    }

    [TestMethod]
    public void InvalidDateRange_ThrowsException()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Should.ThrowAsync<ArgumentException>(() => GetPeriodAsync(today, today.AddDays(-1)));
    }

    [TestMethod]
    public async Task RunningTimeEntry_IsNotReported()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);
        entry.End = null;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.TotalTime.ShouldBe(TimeSpan.Zero);
    }

    [TestMethod]
    public async Task DifferentDay_IsNotReported()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        BuildTimeEntry(today);

        //Act
        var day = await GetDayAsync(yesterday);

        //Assert
        day.Date.ShouldBe(yesterday);
        day.TotalTime.ShouldBe(TimeSpan.Zero);
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
        Should.ThrowAsync<ShouldAssertException>(() => GetPeriodAsync(today, today));

        //Assert
        AlertService.DisableVerifications();
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
        var day = await GetDayAsync(today);

        //Assert
        day.TotalTime.ShouldBe(TimeSpan.FromMinutes(expectedTime));
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
        var day = await GetDayAsync(today);

        //Assert
        day.TotalTime.ShouldBe(TimeSpan.FromMinutes(expectedTime));
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
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeTrue();
        day.ParentPeriod.AllExported.ShouldBeTrue();
    }

    [TestMethod]
    public async Task AllExported_False()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;
        BuildTimeEntry(today).Exported = false;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeFalse();
        day.ParentPeriod.AllExported.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_AlreadyExported_False()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = true;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_ReadyToExport_True()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today).Exported = false;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.CanExport.ShouldBeTrue();
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
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeFalse();
        day.CanExport.ShouldBeFalse();
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
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeFalse();
        day.CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_ProjectDeactivated_False()
    {
        //Arrange
        var today = Today;
        var project = TestActivities.Last().Project?.Copy().With(p => p.Id = Sequence.ProjectId.Next()) ?? throw new InvalidOperationException();
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
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeFalse();
        day.CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_ProjectTbd_False()
    {
        //Arrange
        var today = Today;
        var project = TestActivities.Last().Project?.Copy().With(p => p.Id = Sequence.ProjectId.Next()) ?? throw new InvalidOperationException();
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
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeFalse();
        day.CanExport.ShouldBeFalse();
    }

    [TestMethod]
    public async Task CanExport_CustomerDeactivated_False()
    {
        //Arrange
        var today = Today;
        var customer = TestActivities.Last()?.Project?.Customer.Copy().With(cust => cust.Id = Sequence.CustomerId.Next()) ?? throw new InvalidOperationException();
        customer.Visible = false;
        var project = TestActivities.Last().Project?.Copy().With(p =>
        {
            p.Id = Sequence.ProjectId.Next();
            p.Customer = customer;
        }) ?? throw new InvalidOperationException();
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
        var day = await GetDayAsync(today);

        //Assert
        day.AllExported.ShouldBeFalse();
        day.CanExport.ShouldBeFalse();
    }

    #endregion Exported

    [TestMethod]
    public async Task Url()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today);

        //Act
        var day = await GetDayAsync(today);

        //Assert
        var expected = Kimai.BaseUrl
                .AppendPathSegments(Kimai.CurrentUser.Language, "timesheet")
                .AppendQueryParam("daterange", $"{today:O} - {today:O}")
                .AppendQueryParam("state", 1)  //  & running
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
        var period = await GetPeriodAsync(yesterday, today);

        //Assert
        period.Days.Count.ShouldBe(2);
        period.Days.Single(day => day.Date == yesterday).TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        period.Days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
    }
    
    [TestMethod]
    public async Task MultipleDays_ExportFlagsSet()
    {
        //Arrange
        var today = Today;
        var yesterday = today.AddDays(-1);
        BuildTimeEntry(yesterday, TimeSpan.FromMinutes(10)).With(t => t.Exported = true);
        BuildTimeEntry(today, TimeSpan.FromMinutes(15));
        BuildTimeEntry(today, TimeSpan.FromMinutes(20));

        //Act
        var period = await GetPeriodAsync(yesterday, today);

        //Assert
        period.Days.Count.ShouldBe(2);
        period.Days.Single(day => day.Date == yesterday).AllExported.ShouldBeTrue();
        period.Days.Single(day => day.Date == yesterday).CanExport.ShouldBeFalse();
        period.Days.Single(day => day.Date == today).AllExported.ShouldBeFalse();
        period.Days.Single(day => day.Date == today).CanExport.ShouldBeTrue();
        period.AllExported.ShouldBeFalse();
        period.CanExport.ShouldBeTrue();
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
        var period = await GetPeriodAsync(yesterdayEve, today);

        //Assert
        period.Days.Count.ShouldBe(3);
        period.Days.Single(day => day.Date == yesterdayEve).TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        period.Days.Single(day => day.Date == yesterday).TotalTime.ShouldBe(TimeSpan.Zero);
        period.Days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(20));
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
        var period = await GetPeriodAsync(yesterdayEve, today);

        //Assert
        period.Days.Count.ShouldBe(3);
        period.Days[0].Date.ShouldBe(today);
        period.Days[1].Date.ShouldBe(yesterday);
        period.Days[2].Date.ShouldBe(yesterdayEve);
    }

    [TestMethod]
    public void LimitDateRange()
    {
        var today = Today;
        Should.ThrowAsync<ArgumentException>(() => GetPeriodAsync(today.AddDays(-7), today));
    }

    [TestMethod]
    public void AllowMaxOneWeek()
    {
        var today = Today;
        Should.NotThrowAsync(() => GetPeriodAsync(today.AddDays(-6), today));
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
        var period = await GetPeriodAsync(today, tomorrow);

        //Assert
        period.Days.Count.ShouldBe(1);
        period.Days.Single(day => day.Date == today).TotalTime.ShouldBe(TimeSpan.FromMinutes(20));
    }

    #endregion Multiple Days

    #region Projects

    [TestMethod]
    public async Task EmptyTime_ProjectsEmpty()
    {
        //Arrange
        var today = Today;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ProjectIdProperties()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today, TimeSpan.FromMinutes(24));

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(1);
        var project = day.Projects.Single();
        project.ProjectId.ShouldBe(entry.Project.Id);
        project.ProjectName.ShouldBe(entry.Project.Name);
        project.CustomerId.ShouldBe(entry.Project.Customer.Id);
        project.CustomerName.ShouldBe(entry.Project.Customer.Name);
    }

    [TestMethod]
    public async Task CustomerLogo()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today)
            .Project.Customer.Comment = "Parent Company is Acme Inc.   [Logo](https://example.test/img)";

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(1);
        var project = day.Projects.Single();
        project.CustomerUrl.ShouldNotBeNull();
        project.CustomerUrl.ToString().ShouldBe("https://example.test/img");
    }
    
    [TestMethod]
    public async Task CustomerLogo_None()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today)
            .Project.Customer.Comment = "Parent Company is Acme Inc.";

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(1);
        var project = day.Projects.Single();
        project.CustomerUrl.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task CustomerLogo_Invalid()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today)
            .Project.Customer.Comment = "[Logo](ftp:/missingSlashInvalid/)";

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(1);
        var project = day.Projects.Single();
        project.CustomerUrl.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task CustomerAcronym_Defined()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today)
            .Project.Customer.Name = "Code Monkey Projectiles (CMP)";

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Single().CustomerAcronym.ShouldBe("CMP");
    }
    
    [TestMethod]
    public async Task CustomerAcronym_Undefined()
    {
        //Arrange
        var today = Today;
        BuildTimeEntry(today)
            .Project.Customer.Name = "Code Monkey Projectiles";

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Single().CustomerAcronym.ShouldBeNull();
    }

    [TestMethod]
    public async Task MultipleProjects()
    {
        //Arrange
        var today = Today;
        var project1Activity = TestActivities.SingleRandom();
        var project2Activity1 = TestActivities.Where(a => a.Project?.Id != project1Activity.Project?.Id).SingleRandom();
        var project2Activity2 = TestActivities.Where(a => a.Project?.Id == project2Activity1.Project?.Id && a != project2Activity1).SingleRandom();
        BuildTimeEntry(project1Activity, today, TimeSpan.FromMinutes(10));
        BuildTimeEntry(project2Activity1, today, TimeSpan.FromMinutes(20));
        BuildTimeEntry(project2Activity2, today, TimeSpan.FromMinutes(15));
        project2Activity1.Project.ShouldNotBeNull();
        project1Activity.Project.ShouldNotBeNull();

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(2);
        day.Projects[0].ProjectId.ShouldBe(project2Activity1.Project.Id);
        day.Projects[0].TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
        day.Projects[1].ProjectId.ShouldBe(project1Activity.Project.Id);
        day.Projects[1].TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [TestMethod]
    public async Task MultipleProjectsOrdered()
    {
        //Arrange
        var today = Today;
        var project1Activity = TestActivities.SingleRandom();
        var project2Activity1 = TestActivities.Where(a => a.Project?.Id != project1Activity.Project?.Id).SingleRandom();
        var project2Activity2 = TestActivities.Where(a => a.Project?.Id == project2Activity1.Project?.Id && a != project2Activity1).SingleRandom();
        BuildTimeEntry(project1Activity, today, TimeSpan.FromMinutes(60));
        BuildTimeEntry(project2Activity1, today, TimeSpan.FromMinutes(20));
        BuildTimeEntry(project2Activity2, today, TimeSpan.FromMinutes(15));
        project1Activity.Project.ShouldNotBeNull();
        project2Activity1.Project.ShouldNotBeNull();

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(2);
        day.Projects[0].ProjectId.ShouldBe(project1Activity.Project.Id);
        day.Projects[0].TotalTime.ShouldBe(TimeSpan.FromMinutes(60));
        day.Projects[1].ProjectId.ShouldBe(project2Activity1.Project.Id);
        day.Projects[1].TotalTime.ShouldBe(TimeSpan.FromMinutes(35));
    }

    [TestMethod]
    public async Task ProjectExported()
    {
        //Arrange
        var today = Today;
        var project1Activity = TestActivities.SingleRandom();
        var project2Activity1 = TestActivities.Where(a => a.Project?.Id != project1Activity.Project?.Id).SingleRandom();
        var project2Activity2 = TestActivities.Where(a => a.Project?.Id == project2Activity1.Project?.Id && a != project2Activity1).SingleRandom();
        BuildTimeEntry(project1Activity, today, TimeSpan.FromMinutes(60)).Exported = true;
        BuildTimeEntry(project2Activity1, today, TimeSpan.FromMinutes(20)).Exported = true;
        BuildTimeEntry(project2Activity2, today, TimeSpan.FromMinutes(15)).Exported = false;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects.Length.ShouldBe(2);
        day.Projects[0].AllExported.ShouldBeTrue();
        day.Projects[0].CanExport.ShouldBeFalse();
        day.Projects[1].AllExported.ShouldBeFalse();
        day.Projects[1].CanExport.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ProjectUrl()
    {
        //Arrange
        var today = Today;
        var entry = BuildTimeEntry(today);

        //Act
        var day = await GetDayAsync(today);

        //Assert
        var expected = Kimai.BaseUrl
            .AppendPathSegments(Kimai.CurrentUser.Language, "timesheet")
            .AppendQueryParam("daterange", $"{today:O} - {today:O}")
            .AppendQueryParam("state", 1)  // stopped & running
            .AppendQueryParam("billable", 0)
            .AppendQueryParam("exported", 1)
            .AppendQueryParam("orderBy", "begin")
            .AppendQueryParam("order", "DESC")
            .AppendQueryParam("searchTerm", string.Empty)
            .AppendQueryParam("performSearch", "performSearch")
            .AppendQueryParam("projects[]", entry.Project.Id)
            .ToUri();
        day.Projects[0].Url.ShouldBe(expected);
    }

    #endregion Projects

    #region Activities

    [TestMethod]
    public async Task MultipleActivities()
    {
        //Arrange
        var today = Today;
        var project = TestActivities.SingleRandom().Project;
        var activities = TestActivities.Where(a => a.Project?.Id == project?.Id).ToArray();
        BuildTimeEntry(activities[0], today, TimeSpan.FromMinutes(20)).Exported = true;
        BuildTimeEntry(activities[1], today, TimeSpan.FromMinutes(10)).Exported = false;
        BuildTimeEntry(activities[2], today, TimeSpan.FromMinutes(15)).Exported = true;
        BuildTimeEntry(activities[2], today, TimeSpan.FromMinutes(35)).Exported = false;

        //Act
        var day = await GetDayAsync(today);

        //Assert
        day.Projects[0].Activities.Length.ShouldBe(3);
        day.Projects[0].Activities[0].ActivityId.ShouldBe(activities[2].Id);
        day.Projects[0].Activities[0].ActivityName.ShouldBe(activities[2].Name);
        day.Projects[0].Activities[0].ActivityDescription.ShouldBe(activities[2].Comment);
        day.Projects[0].Activities[0].TotalTime.ShouldBe(TimeSpan.FromMinutes(50));
        day.Projects[0].Activities[0].AllExported.ShouldBeFalse();
        day.Projects[0].Activities[0].CanExport.ShouldBeTrue();

        day.Projects[0].Activities[1].ActivityId.ShouldBe(activities[0].Id);
        day.Projects[0].Activities[1].TotalTime.ShouldBe(TimeSpan.FromMinutes(20));
        day.Projects[0].Activities[1].AllExported.ShouldBeTrue();
        day.Projects[0].Activities[1].CanExport.ShouldBeFalse();

        day.Projects[0].Activities[2].ActivityId.ShouldBe(activities[1].Id);
        day.Projects[0].Activities[2].TotalTime.ShouldBe(TimeSpan.FromMinutes(10));
        day.Projects[0].Activities[2].AllExported.ShouldBeFalse();
        day.Projects[0].Activities[2].CanExport.ShouldBeTrue();

        activities[1].Project.ShouldNotBeNull();

        var expected = Kimai.BaseUrl
            .AppendPathSegments(Kimai.CurrentUser.Language, "timesheet")
            .AppendQueryParam("daterange", $"{today:O} - {today:O}")
            .AppendQueryParam("state", 1)  // stopped & running
            .AppendQueryParam("billable", 0)
            .AppendQueryParam("exported", 1)
            .AppendQueryParam("orderBy", "begin")
            .AppendQueryParam("order", "DESC")
            .AppendQueryParam("searchTerm", string.Empty)
            .AppendQueryParam("performSearch", "performSearch")
            .AppendQueryParam("projects[]", activities[1].Project?.Id)
            .AppendQueryParam("activities[]", activities[1].Id)
            .ToUri();
        day.Projects[0].Activities[2].Url.ShouldBe(expected);
    }

    #endregion Activities

}
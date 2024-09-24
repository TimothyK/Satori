using Autofac;
using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using Satori.Kimai.Models;
using Satori.Kimai.Tests.Extensions;
using Shouldly;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class TimeSheetTests
{
    #region Helpers

    #region Arrange

    private readonly ConnectionSettings _connectionSettings = Globals.Services.Scope.Resolve<ConnectionSettings>();

    private Url GetUrl(TimeSheetFilter filter) =>
        _connectionSettings.Url
            .AppendPathSegment("api/timesheets")
            .AppendQueryParam("full", "true")
            .AppendQueryParams(filter);

    private readonly MockHttpMessageHandler _mockHttp = Globals.Services.Scope.Resolve<MockHttpMessageHandler>();

    private void SetResponse(Url url, byte[] response)
    {
         _mockHttp.WhenIsFullUrl(url)
            .Respond("application/json", System.Text.Encoding.Default.GetString(response));
    }

    #endregion Arrange

    #region Act

    private TimeEntry GetTimeEntry(int id)
    {
        //Arrange
        var filter = Builder.Builder<TimeSheetFilter>.New().Build();
        SetResponse(GetUrl(filter), SampleFiles.SampleResponses.ThreeEntries);

        //Act
        var srv = Globals.Services.Scope.Resolve<IKimaiServer>();
        return srv.GetTimeSheetAsync(filter).Result.Single(entry => entry.Id == id);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod] public void ASmokeTest() => GetTimeEntry(3323).Id.ShouldBe(3323);
    [TestMethod] public void Description() => GetTimeEntry(3323).Description.ShouldBe("D#12345 Fix bug » D#12346 Testing \r\n🏆 I fixed the bug");
    [TestMethod] public void Exported() => GetTimeEntry(3323).Exported.ShouldBeFalse();
    
    [TestMethod] public void User() => GetTimeEntry(3323).User.Id.ShouldBe(2);

    [TestMethod] public void ActivityId_Entry3323() => GetTimeEntry(3323).Activity.Id.ShouldBe(9696);
    [TestMethod] public void ActivityNullProject() => GetTimeEntry(3323).Activity.Project.ShouldBeNull();

    [TestMethod] public void ActivityId_Entry3322() => GetTimeEntry(3322).Activity.Id.ShouldBe(9453);
    [TestMethod] public void ActivityProjectId() => GetTimeEntry(3322).Activity.Project.Id.ShouldBe(337);
    [TestMethod] public void ActivityName() => GetTimeEntry(3322).Activity.Name.ShouldBe("1.2.3 Custom Software Development » March 2024");
    [TestMethod] public void ActivityComment() => GetTimeEntry(3322).Activity.Comment.ShouldBe("Development for March [TaskKey=123]");
    [TestMethod] public void ActivityVisible() => GetTimeEntry(3322).Activity.Visible.ShouldBeTrue();

    [TestMethod] public void ProjectId() => GetTimeEntry(3322).Project.Id.ShouldBe(337);
    [TestMethod] public void ProjectName() => GetTimeEntry(3322).Project.Name.ShouldBe("1234 Acme Web App Enhancements");
    [TestMethod] public void ProjectComment() => GetTimeEntry(3322).Project.Comment.ShouldBe("PO#1234");
    [TestMethod] public void ProjectVisible() => GetTimeEntry(3322).Project.Visible.ShouldBeTrue();
    [TestMethod] public void ProjectGlobalActivities() => GetTimeEntry(3322).Project.GlobalActivities.ShouldBeFalse();

    [TestMethod] public void CustomerID() => GetTimeEntry(3322).Project.Customer.Id.ShouldBe(2);
    [TestMethod] public void CustomerName() => GetTimeEntry(3322).Project.Customer.Name.ShouldBe("Acme Inc");
    [TestMethod] public void CustomerNumber() => GetTimeEntry(3322).Project.Customer.Number.ShouldBe("AcmeFsk");
    [TestMethod] public void CustomerComment() => GetTimeEntry(3322).Project.Customer.Comment.ShouldBeNull();
    [TestMethod] public void CustomerVisible() => GetTimeEntry(3322).Project.Customer.Visible.ShouldBeTrue();


}
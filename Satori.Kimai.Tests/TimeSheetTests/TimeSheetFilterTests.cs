using Flurl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Satori.Kimai.Models;
using Shouldly;

namespace Satori.Kimai.Tests.TimeSheetTests;

[TestClass]
public class TimeSheetFilterTests
{
    private static Url BuildUrlWith(Action<TimeSheetFilter> setup)
    {
        //Arrange
        var filter = new TimeSheetFilter();
        setup(filter);

        var url = new Url(new Uri("http://kimai.test"));

        //Act
        return url.AppendQueryParams(filter);
    }


    [TestMethod] public void ASmokeTest_NoFilters_NoQueryParams() => BuildUrlWith(_ => { }).Query.ShouldBeNullOrEmpty();
    
    
    [TestMethod] 
    public void Begin() => 
        BuildUrlWith(filter => filter.Begin = new DateTime(2024, 5, 14, 13, 34, 56, DateTimeKind.Unspecified))
        .Query.ShouldBe("begin=2024-05-14T13%3A34%3A56");

    [TestMethod] 
    public void Begin_Utc() => 
        BuildUrlWith(filter => filter.Begin = new DateTime(2024, 5, 14, 13, 34, 56, DateTimeKind.Utc))
        .Query.ShouldBe("begin=2024-05-14T13%3A34%3A56");
    
    [TestMethod] 
    public void Begin_Local() => 
        BuildUrlWith(filter => filter.Begin = new DateTime(2024, 5, 14, 13, 34, 56, DateTimeKind.Local))
        .Query.ShouldBe("begin=2024-05-14T13%3A34%3A56");

    [TestMethod]
    public void End() =>
        BuildUrlWith(filter => filter.End = new DateTime(2024, 5, 14, 13, 34, 56))
            .Query.ShouldBe("end=2024-05-14T13%3A34%3A56");


    [TestMethod] public void Active() => BuildUrlWith(filter => filter.IsRunning = true).Query.ShouldBe("active=1");
    [TestMethod] public void Stopped() => BuildUrlWith(filter => filter.IsRunning = false).Query.ShouldBe("active=0");
    [TestMethod] public void Term() => BuildUrlWith(filter => filter.Term = "D#12345").Query.ShouldBe("term=D%2312345");
    [TestMethod] public void Page() => BuildUrlWith(filter => filter.Page = 2).Query.ShouldBe("page=2");
    [TestMethod] public void Size() => BuildUrlWith(filter => filter.Size = 1).Query.ShouldBe("size=1");
}
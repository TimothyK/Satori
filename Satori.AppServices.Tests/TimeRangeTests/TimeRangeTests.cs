using Shouldly;

namespace Satori.AppServices.Tests.TimeRangeTests;

[TestClass]
public class TimeRangeTests
{
    #region Helpers

    private static readonly DateTimeOffset Root = new(2025, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeRange TestRange1 = new(Root, Root.AddHours(1));
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    private static void CompareMessage(TimeRange testRange2)
    {
        Console.WriteLine($"Comparing");
        Console.WriteLine($"{TestRange1} to");
        Console.WriteLine($"{testRange2}");
    }

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest_IsAfter()
    {
        // |TestRange1|
        //            |testRange2|
        var testRange2 = TestRange1.NextBlock();

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeFalse();
    }

    [TestMethod]
    public void IsBefore()
    {
        //            |TestRange1|
        // |testRange2|
        var testRange2 = TestRange1.PreviousBlock();

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeFalse();
    }
    
    [TestMethod]
    public void IsEqual()
    {
        // |TestRange1|
        // |testRange2|
        var testRange2 = TestRange1;

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeTrue();
    }
    
    [TestMethod]
    public void NullEndDate_NeverOverlapping()
    {
        var testRange2 = new TimeRange(TestRange1.Begin, null);

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeFalse();
        testRange2.TestIsOverlapping(TestRange1).ShouldBeFalse();
    }

    [TestMethod]
    public void InvalidRange_ThrowsInvalidOp()
    {
        var testRange2 = new TimeRange(Root + OneMinute, Root);  //End is before Begin

        Should.Throw<InvalidOperationException>(() => TestRange1.TestIsOverlapping(testRange2));
        Should.Throw<InvalidOperationException>(() => testRange2.TestIsOverlapping(TestRange1));
    }


    
    [TestMethod]
    public void Overlapping_OnEnd()
    {
        // |TestRange1|
        //           |testRange2|
        var testRange2 = TestRange1.NextBlock() - TimeSpan.FromMinutes(1);

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeTrue();
    }
    
    [TestMethod]
    public void Overlapping_OnBegin()
    {
        //  |TestRange1|
        // |testRange2|
        var testRange2 = TestRange1 - TimeSpan.FromMinutes(1);

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeTrue();
    }
    
    [TestMethod]
    public void Overlapping_WhollyContained()
    {
        // | TestRange1 |
        //  |testRange2|
        var testRange2 = new TimeRange(TestRange1.Begin + OneMinute, TestRange1.End - OneMinute);

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeTrue();
    }
    
    [TestMethod]
    public void Overlapping_WhollyEncapsulated()
    {
        //  | TestRange1 |
        // |  testRange2  |
        var testRange2 = new TimeRange(TestRange1.Begin - OneMinute, TestRange1.End + OneMinute);

        CompareMessage(testRange2);
        TestRange1.TestIsOverlapping(testRange2).ShouldBeTrue();
    }
    
}
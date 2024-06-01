using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Shouldly;

namespace Satori.AppServices.Tests.CompletedWorkTests;

[TestClass]
public class CompletedWorkTests
{
    public CompletedWorkTests()
    {
        AzureDevOpsBuilder = AzureDevOps.CreateBuilder();
    }

    #region Helpers

    #region Arrange

    private TestAzureDevOpsServer AzureDevOps { get; } = new();
    private AzureDevOpsDatabaseBuilder AzureDevOpsBuilder { get; }

    #endregion Arrange

    #region Act

    private async Task AdjustCompletedWorkAsync(int workItemId, double adjustment)
    {
        var srv = new CompletedWorkService(AzureDevOps.AsInterface());
        await srv.AdjustCompletedWorkAsync(workItemId, adjustment);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.CompletedWork = null;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.CompletedWork.ShouldBe(adjustment);
    }
    
    [TestMethod]
    public void NonTask_ThrowsInvalidOp()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem(out var workItem);
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        Should.ThrowAsync<InvalidOperationException>(() => AdjustCompletedWorkAsync(workItem.Id, adjustment))
            .Result.Message.ShouldContain("not a task");
    }
    
    [TestMethod]
    public void UnknownWorkItemId_ThrowsInvalidOp()
    {
        // Arrange
        var workItemId = RandomGenerator.Integer(100000);
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        Should.ThrowAsync<InvalidOperationException>(() => AdjustCompletedWorkAsync(workItemId, adjustment))
            .Result.Message.ShouldBe($"Work Item ID {workItemId} was not found");
    }
    
    [TestMethod]
    public async Task Adjustment_Increments()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.CompletedWork = 1.2;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.CompletedWork.ShouldBe(1.2 + adjustment);
    }
    
    [TestMethod]
    public async Task RevisionIncrements()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);
        var originalRev = task.Rev;

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Rev.ShouldBe(originalRev + 1);
    }
    
    [TestMethod]
    public async Task AdjustmentZero_RevUnchanged()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.CompletedWork = 1.2;
        const double adjustment = 0.0;
        var originalRev = task.Rev;

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Rev.ShouldBe(originalRev);
    }
    
    [TestMethod]
    public async Task OriginalEstimateNull_SetToOriginalRemaining()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.OriginalEstimate = null;
        var original = RandomGenerator.Number(8).ToNearest(0.05);
        task.Fields.RemainingWork = original;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);
        
        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.OriginalEstimate.ShouldBe(original);
    }
    
    [TestMethod]
    public async Task OriginalEstimateAndRemainingNull_OriginalNotUpdated()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);
        
        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.OriginalEstimate.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task NullRemaining_IsNotChanged()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.OriginalEstimate = null;
        task.Fields.RemainingWork = null;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);
        
        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.RemainingWork.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task DecrementRemaining()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var original = RandomGenerator.Number(8).ToNearest(0.05);
        task.Fields.RemainingWork = original;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);
        
        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.RemainingWork.ShouldBe(original - adjustment);
    }

    [TestMethod]
    public async Task NullRemaining_DecrementFromOriginalEstimate()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var original = RandomGenerator.Number(8).ToNearest(0.05);
        task.Fields.OriginalEstimate = original;
        task.Fields.RemainingWork = null;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.RemainingWork.ShouldBe(original - adjustment);
    }
}
﻿using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.ViewModels.WorkItems;
using Shouldly;

namespace Satori.AppServices.Tests.CompletedWorkTests;

[TestClass]
public class CompletedWorkTests
{
    /// <summary>
    /// Remaining work can be shown on the Azure DevOps cards on the sprint board.
    /// This value is usually cut off if it is 2 decimal points.  The adjustments should for the Remaining Work to be rounded to 1 decimal point.
    /// </summary>
    private const double RemainingWorkPrecision = 0.1;
    /// <summary>
    /// Completed work isn't usually shown on task cards, so we can have extra precision here.
    /// </summary>
    private const double CompletedWorkPrecision = 0.05;

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
        var adjustment = RandomGenerator.Number(2.5).ToNearest(CompletedWorkPrecision);

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
    public void UnknownWorkItemId_Throws()
    {
        // Arrange
        var workItemId = RandomGenerator.Integer(100000);
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        var ex = Should.ThrowAsync<Exception>(() => AdjustCompletedWorkAsync(workItemId, adjustment)).Result;

        // Assert
        ex.Message.ShouldContain(workItemId.ToString());
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
        task.Fields.CompletedWork.ShouldBe((1.2 + adjustment).ToNearest(CompletedWorkPrecision));
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
        task.Fields.RemainingWork = 2.3;
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
        var adjustment = RandomGenerator.Number(2.5).ToNearest(RemainingWorkPrecision);
        
        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.RemainingWork.ShouldBe((original - adjustment).ToNearest(RemainingWorkPrecision));
    }

    [TestMethod]
    public async Task NullRemaining_DecrementFromOriginalEstimate()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var original = RandomGenerator.Number(8).ToNearest(RemainingWorkPrecision);
        task.Fields.OriginalEstimate = original;
        task.Fields.RemainingWork = null;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(RemainingWorkPrecision);

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.RemainingWork.ShouldBe((original - adjustment).ToNearest(RemainingWorkPrecision));
    }
    
    [TestMethod]
    public async Task Done_DoesNotSetRemainingWork()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        var original = RandomGenerator.Number(8).ToNearest(RemainingWorkPrecision);
        task.Fields.OriginalEstimate = original;
        task.Fields.RemainingWork = original;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(RemainingWorkPrecision);
        
        task.Fields.State = ScrumState.Done.ToApiValue();

        // Act
        await AdjustCompletedWorkAsync(task.Id, adjustment);

        // Assert
        task.Fields.RemainingWork.ShouldBe(original);
    }
}
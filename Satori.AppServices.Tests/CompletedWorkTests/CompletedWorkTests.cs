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

    private async Task AdjustCompletedWork(int workItemId, double adjustment)
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
        await AdjustCompletedWork(task.Id, adjustment);

        // Assert
        task.Fields.CompletedWork.ShouldBe(adjustment);
    }
    
    [TestMethod]
    public async Task Adjustment_Increments()
    {
        // Arrange
        AzureDevOpsBuilder.BuildWorkItem().AddChild(out var task);
        task.Fields.CompletedWork = 1.2;
        var adjustment = RandomGenerator.Number(2.5).ToNearest(0.05);

        // Act
        await AdjustCompletedWork(task.Id, adjustment);

        // Assert
        task.Fields.CompletedWork.ShouldBe(1.2 + adjustment);
    }
    }
    }
}
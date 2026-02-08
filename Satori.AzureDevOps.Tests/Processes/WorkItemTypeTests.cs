using Flurl;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Satori.AzureDevOps.Models;
using Satori.AzureDevOps.Tests.Globals;
using Satori.AzureDevOps.Tests.Processes.SampleFiles;
using Shouldly;

namespace Satori.AzureDevOps.Tests.Processes;

[TestClass]
public class WorkItemTypeTests
{
    private readonly ServiceProvider _serviceProvider;

    public WorkItemTypeTests()
    {
        var services = new AzureDevOpsServiceCollection();
        _serviceProvider = services.BuildServiceProvider();

        _connectionSettings = _serviceProvider.GetRequiredService<ConnectionSettings>();
        _mockHttp = _serviceProvider.GetRequiredService<MockHttpMessageHandler>();
        _mockHttp.Clear();
    }

    #region Helpers

    #region Arrange

    private readonly Guid _scrumProcessId = new("6b724908-ef14-45cf-84f8-768b5384da45");
    private readonly ConnectionSettings _connectionSettings;

    private Url GetUrl(Guid processId) =>
        _connectionSettings.Url
            .AppendPathSegment("_apis/processes")
            .AppendPathSegment(processId)
            .AppendPathSegment("workItemTypes")
            .AppendQueryParam("$expand", "states")
            .AppendQueryParam("api-version", "7.1");

    private readonly MockHttpMessageHandler _mockHttp;

    private void SetResponse(Url url, string response)
    {
        _mockHttp.When(url).Respond("application/json", response);
    }

    #endregion Arrange

    #region Act

    private async Task<WorkItemType[]> GetWorkItemTypesAsync(Guid processId)
    {
        //Arrange
        var response = processId == _scrumProcessId ? Responses.ScrumWorkItemTypes 
            : throw new InvalidOperationException();
        SetResponse(GetUrl(processId), response);

        //Act
        var srv = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        return await srv.GetWorkItemTypesAsync(processId);
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public async Task ASmokeTest() => (await GetWorkItemTypesAsync(_scrumProcessId)).Length.ShouldBe(9);

    [TestMethod]
    public async Task HasTask()
    {
        //Act
        var task = (await GetWorkItemTypesAsync(_scrumProcessId))
            .Single(t => t.Name == "Task");

        //Assert
        task.Color.ShouldBe("A4880A");
        task.ReferenceName.ShouldBe("Microsoft.VSTS.WorkItemTypes.Task");
        task.IsDisabled.ShouldBeFalse();
        task.States.Length.ShouldBe(4);

        var toDo = task.States.Single(state => state.Name == "To Do");
        toDo.Id.ShouldBe(new Guid("a07f4091-2949-44a3-9613-d84f3733eb1b"));
        toDo.StateCategory.ShouldBe("Proposed");
        toDo.Order.ShouldBe(1);

        var removed = task.States.Single(state => state.Name == "Removed");
        removed.Id.ShouldBe(new Guid("0293a2ce-2a42-4d0e-bbbf-d2237efa0db8"));
        removed.StateCategory.ShouldBe("Removed");
        removed.Order.ShouldBe(4);
    }
}
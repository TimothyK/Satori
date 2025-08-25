using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Services;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.Kimai.Utilities;
using Shouldly;
using WorkItem = Satori.AzureDevOps.Models.WorkItem;

namespace Satori.AppServices.Tests.PullRequests;

/// <summary>
/// Tests the WorkItem view model that is returned from <seealso cref="PullRequestService.GetPullRequestsAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The WorkItem view model returned from here has fewer properties populated then if the WorkItem was read from the WorkItemService.
/// The WorkItemService give more details related to the child tasks and parents, and the pull requests belongs to the work item.
/// This is simply the work items directly linked to the PR.
/// </para>
/// </remarks>
[TestClass]
public class WorkItemTests
{
    private readonly ServiceProvider _serviceProvider;

    private Uri AzureDevOpsRootUrl => _serviceProvider.GetRequiredService<IAzureDevOpsServer>().ConnectionSettings.Url;

    public WorkItemTests()
    {
        var services = new SatoriServiceCollection();
        services.AddTransient<PullRequestService>();
        _serviceProvider = services.BuildServiceProvider();
    }

    #region Helpers

    #region Arrange

    private WorkItem? _expected;

    private WorkItem Expected
    {
        get
        {
            if (_expected != null)
            {
                return _expected;
            }

            var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
            builder.BuildPullRequest().WithWorkItem(out _expected);
            return _expected;
        }
    }

    #endregion Arrange

    #region Act

    private IEnumerable<ViewModels.WorkItems.WorkItem> GetWorkItems()
    {
        var srv = _serviceProvider.GetRequiredService<PullRequestService>();
        var pullRequests = srv.GetPullRequestsAsync().Result.ToArray();
        srv.AddWorkItemsToPullRequestsAsync(pullRequests).GetAwaiter().GetResult();
        return [.. pullRequests.Single().WorkItems];
    }

    private ViewModels.WorkItems.WorkItem GetSingleWorkItem()
    {
        _ = Expected;  //Force lazy load of expected WorkItem

        return GetWorkItems().Single();
    }

    #endregion Act

    #endregion Helpers

    [TestMethod]
    public void ASmokeTest()
    {
        //Arrange
        var expected = Expected;

        //Act
        var actual = GetSingleWorkItem();

        //Assert
        actual.Id.ShouldBe(expected.Id);
    }

    [TestMethod] public void Title() => GetSingleWorkItem().Title.ShouldBe(Expected.Fields.Title);
    
    [TestMethod] 
    public void AssignedTo()
    {
        var expected = Expected.Fields.AssignedTo ?? throw new InvalidOperationException();

        var actual = GetSingleWorkItem().AssignedTo;

        actual.ShouldNotBeNull();
        actual.AzureDevOpsId.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ToString().ShouldBe(expected.ImageUrl);
    }
    
    [TestMethod] 
    public void CreatedBy()
    {
        var expected = Expected.Fields.CreatedBy;

        var actual = GetSingleWorkItem().CreatedBy;

        actual.ShouldNotBeNull();
        actual.AzureDevOpsId.ShouldBe(expected.Id);
        actual.DisplayName.ShouldBe(expected.DisplayName);
        actual.AvatarUrl.ToString().ShouldBe(expected.ImageUrl);
    }

    [TestMethod] public void CreatedDate() => GetSingleWorkItem().CreatedDate.ShouldBe(Expected.Fields.SystemCreatedDate);

    [TestMethod] public void IterationPath() => GetSingleWorkItem().IterationPath.ShouldBe(Expected.Fields.IterationPath);

    
    [TestMethod]
    [DataRow("Bug")]
    [DataRow("Product Backlog Item")]
    [DataRow("Task")]
    [DataRow("Feature")]
    [DataRow("Epic")]
    [DataRow("garbage")]
    public void Type(string type)
    {
        //Arrange
        var workItem = Expected;
        workItem.Fields.WorkItemType = type;
        var expected = WorkItemType.FromApiValue(type);

        //Act
        var actual = GetSingleWorkItem();

        //Assert
        actual.Type.ShouldBe(expected);
    }


    [TestMethod] public void State() => GetSingleWorkItem().State.ToApiValue().ShouldBe(Expected.Fields.State);
    
    [TestMethod] public void ProjectCode()
    {
        //Arrange
        var kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();
        var project = kimai.AddProject();

        var workItem = Expected;
        workItem.Fields.ProjectCode = ProjectCodeParser.GetProjectCode(project.Name);

        //Act
        var actual = GetSingleWorkItem();

        //Assert
        actual.KimaiProject.ShouldNotBeNull();
        actual.KimaiProject.Id.ShouldBe(project.Id);
    }

    [TestMethod] public void Url() => GetSingleWorkItem().Url.ShouldBe(AzureDevOpsRootUrl + "/_workItems/edit/" + Expected.Id);
}
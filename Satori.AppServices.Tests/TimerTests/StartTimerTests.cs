using CodeMonkeyProjectiles.Linq;
using Microsoft.Extensions.DependencyInjection;
using Satori.AppServices.Extensions;
using Satori.AppServices.Services;
using Satori.AppServices.Services.Converters;
using Satori.AppServices.Tests.TestDoubles;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps;
using Satori.AppServices.Tests.TestDoubles.AzureDevOps.Builders;
using Satori.AppServices.Tests.TestDoubles.Kimai;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AzureDevOps;
using Satori.Kimai;
using Satori.Kimai.Models;
using Satori.TimeServices;
using Shouldly;
using Activity = Satori.Kimai.ViewModels.Activity;

namespace Satori.AppServices.Tests.TimerTests;

[TestClass]
public class StartTimerTests
{
    private readonly ServiceProvider _serviceProvider;

    public StartTimerTests()
    {
        Person.Me = null;  //Reset cache

        var serviceCollection = new SatoriServiceCollection();
        serviceCollection.AddSingleton<UserService>();
        serviceCollection.AddScoped<TimerService>();
        _serviceProvider = serviceCollection.BuildServiceProvider();

        _serviceProvider.GetRequiredService<TestAzureDevOpsServer>().RequireRecordLocking = false;
    }

    #region Helpers

    #region Arrange

    private async Task<Activity> BuildActivityAsync()
    {
        var kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();
        var activity = kimai.AddActivity();

        var customers = await kimai.AsInterface().GetCustomersAsync();
        var viewModel = customers
            .SelectMany(customer => customer.Projects)
            .SelectMany(project => project.Activities)
            .Single(a => a.Id == activity.Id);

        return viewModel;
    }

    private async Task<WorkItem> BuildTask(Action<AzureDevOps.Models.WorkItem>? arrange = null)
    {
        var builder = _serviceProvider.GetRequiredService<AzureDevOpsDatabaseBuilder>();
        builder.BuildWorkItem(out var workItem)
            .AddChild(out var task);

        arrange?.Invoke(task);

        var kimai = _serviceProvider.GetRequiredService<IKimaiServer>();

        var taskViewModel = await task.ToViewModelAsync(kimai);
        taskViewModel.Parent = await workItem.ToViewModelAsync(kimai);

        return taskViewModel;
    }

    private TimeEntry BuildTimeEntry()
    {
        var kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();

        var lastEntry = kimai.GetLastEntry();
        if (lastEntry != null && lastEntry.End == null)
        {
            var duration = RandomGenerator.TimeSpan(TimeSpan.FromMinutes(30)).ToNearest(TimeSpan.FromMinutes(3));
            lastEntry.End = lastEntry.Begin + duration;
        }
        var startTime = lastEntry?.End ?? DateTimeOffset.Now.AddHours(-27).TruncateSeconds();

        var entry = Builder.Builder<TimeEntry>.New().Build(t =>
        {
            t.Id = Sequence.TimeEntryId.Next();
            t.User = kimai.CurrentUser;
            t.Begin = startTime;
            t.End = null;
            t.Activity = lastEntry?.Activity ?? BuildActivity();
        }, int.MaxValue);
        entry.Project = entry.Activity.Project ?? BuildProject();
        
        kimai.AddTimeEntry(entry);

        return entry;
    }

    private static Kimai.Models.Activity BuildActivity()
    {
        return Builder.Builder<Kimai.Models.Activity>.New().Build(a => a.Id = Sequence.ActivityId.Next(), int.MaxValue);
    }
    
    private static Project BuildProject()
    {
        return Builder.Builder<Project>.New().Build(p => p.Id = Sequence.ProjectId.Next(), int.MaxValue);
    }

    #endregion Arrange

    #region Act

    private async Task StartTimerAsync(WorkItem task, Activity activity)
    {
        var srv = _serviceProvider.GetRequiredService<TimerService>();
        await srv.StartTimerAsync(task, activity);
    }

    #endregion Act

    #region Assert

    private TimeEntry? GetLastTimeEntry()
    {
        var kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();
        var timeEntry = kimai.GetLastEntry();
        return timeEntry;
    }

    private async Task<WorkItem> RefreshTaskAsync(WorkItem task)
    {
        var azureDevOpsServer = _serviceProvider.GetRequiredService<IAzureDevOpsServer>();
        var actual = await azureDevOpsServer.GetWorkItemsAsync(task.Id);

        var kimai = _serviceProvider.GetRequiredService<IKimaiServer>();
        return await actual.Single().ToViewModelAsync(kimai);
    }

    #endregion Assert

    #endregion Helpers

    #region Smoke Tests

    [TestMethod]
    public async Task ASmokeTest()
    {
        // Arrange
        var task = await BuildTask();
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var timeEntry = GetLastTimeEntry();
        timeEntry.ShouldNotBeNull();
    }
    
    [TestMethod]
    public async Task TimeEntryIsRunning()
    {
        // Arrange
        var task = await BuildTask();
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var timeEntry = GetLastTimeEntry();
        timeEntry.ShouldNotBeNull();

        timeEntry.End.ShouldBeNull();
    }
    
    [TestMethod]
    public async Task LinkedToActivity()
    {
        // Arrange
        var task = await BuildTask();
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var timeEntry = GetLastTimeEntry();
        timeEntry.ShouldNotBeNull();

        timeEntry.Activity.Id.ShouldBe(activity.Id);
        timeEntry.Project.Id.ShouldBe(activity.Project.Id);
    }

    [TestMethod]
    public async Task DescriptionLinksToWorkItem()
    {
        // Arrange
        var task = await BuildTask();
        task.Parent.ShouldNotBeNull();
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var timeEntry = GetLastTimeEntry();
        timeEntry.ShouldNotBeNull();

        timeEntry.Description.ShouldNotBeNull();
        timeEntry.Description.ShouldContain($"D#{task.Id}");
        timeEntry.Description.ShouldBe(task.ToKimaiDescription());
        timeEntry.Description.ShouldBe($"D#{task.Parent.Id} {task.Parent.Title} » D#{task.Id} {task.Title}");
    }

    #endregion Smoke Tests

    #region Begin Time

    [TestMethod]
    public async Task Begin_NoRunningTask_SetToNow()
    {
        // Arrange
        var task = await BuildTask();
        var activity = await BuildActivityAsync();
        var timeServer = _serviceProvider.GetRequiredService<TestTimeServer>();
        var t = DateTimeOffset.Now 
                + RandomGenerator.TimeSpan(TimeSpan.FromHours(1)); //Randomize just to ensure the TimeServer is used.
        timeServer.SetTime(t);

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var timeEntry = GetLastTimeEntry();
        timeEntry.ShouldNotBeNull();

        timeEntry.Begin.ShouldBe(t.TruncateSeconds());
    }
    
    [TestMethod]
    public async Task StopsRunningTaskAndSetsCutoverTimeToWhatWasReturnedFromKimaiOnStop()
    {
        // Arrange
        // This running task should be stopped when the new time entry is started
        var runningEntry = BuildTimeEntry();
        runningEntry.End.ShouldBeNull(); //IsRunning

        // Kimai automatically assigns the end time when stopping the running task.
        // The End time reflects the current time on the Kimai server.
        // The start (Begin) time of the new time entry should reflect that time.
        var kimai = _serviceProvider.GetRequiredService<TestKimaiServer>();
        var timeServer = new TestTimeServer();
        kimai.TimeServer = timeServer;

        var stopTime = DateTimeOffset.UtcNow.AddMinutes(3).TruncateSeconds();
        timeServer.SetTime(stopTime);

        var task = await BuildTask();
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var timeEntry = GetLastTimeEntry();
        timeEntry.ShouldNotBeNull();

        timeEntry.Begin.ShouldBe(stopTime);

        runningEntry.End.ShouldNotBeNull();
        runningEntry.End.Value.ShouldBe(stopTime);
    }

    #endregion Begin Time

    #region Update Task

    [TestMethod]
    public async Task TaskIsSetToInProgress()
    {
        // Arrange
        var task = await BuildTask(t => t.Fields.State = ScrumState.ToDo.ToApiValue());
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var actual = await RefreshTaskAsync(task);
        actual.State.ShouldBe(ScrumState.InProgress);
        actual.Rev.ShouldBe(task.Rev + 1);
    }
    
    [TestMethod]
    public async Task TaskProjectCodeSet()
    {
        // Arrange
        var task = await BuildTask(t => t.Fields.ProjectCode = null);
        var activity = await BuildActivityAsync();

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var actual = await RefreshTaskAsync(task);
        actual.KimaiActivity.ShouldNotBeNull();
        actual.KimaiActivity.Id.ShouldBe(activity.Id);
        actual.Rev.ShouldBe(task.Rev + 1);
    }
    
    [TestMethod]
    public async Task NoChanges_TaskNotUpdated()
    {
        // Arrange
        var activity = await BuildActivityAsync();
        var task = await BuildTask(t =>
        {
            t.Fields.ProjectCode = activity.Project.ProjectCode + "." + activity.ActivityCode;
            t.Fields.State = ScrumState.InProgress.ToApiValue();
        });

        // Act
        await StartTimerAsync(task, activity);

        // Assert
        var actual = await RefreshTaskAsync(task);
        actual.Rev.ShouldBe(task.Rev);
    }

    #endregion Update Task
}
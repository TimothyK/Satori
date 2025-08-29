using Blazored.LocalStorage;
using CodeMonkeyProjectiles.Linq;
using Flurl;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Satori.AppServices.Services.Abstractions;
using Satori.AppServices.ViewModels;
using Satori.AppServices.ViewModels.Abstractions;
using Satori.AppServices.ViewModels.PullRequests.ActionItems;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.AppServices.ViewModels.WorkItems.ActionItems;
using Satori.TimeServices;
using Satori.Utilities;
using Toolbelt.Blazor.HotKeys2;

namespace Satori.Pages.SprintBoard;

public partial class SprintBoards
{
    private Sprint[]? _sprints;
    private WorkItem[]? _workItems;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        PriorityAdjustment = new PriorityAdjustmentViewModel([], AlertService);
    }

    protected override async Task OnInitializedAsync()
    {
        if (!ConnectionSettingsStore.GetAzureDevOpsSettings().Enabled)
        {
            // This page shouldn't be accessible if Kimai is disabled.  Go to Home page where AzureDevOps can be configured/enabled.
            NavigationManager.NavigateTo("/");
        }

        WithFilterInitializeFromUrl();
        ActionItemFilterInitializeFromUrl();

        var sprints = (await SprintBoardService.GetActiveSprintsAsync()).ToArray();
        TeamSelection = new TeamSelectionViewModel(sprints, NavigationManager);
        TeamSelection.SelectedTeamChanged += ResetWorkItemCounts;
        _sprints = sprints;
        StateHasChanged();

        await RefreshAsync();
    }


    private bool _isInitialized;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HotKeys.CreateContext()
                .Add(ModCode.Alt, Code.P, EnterAdjustPriorityMode, new HotKeyOptions { Description = "Adjust Priorities" })
                .Add(ModCode.None, Code.Escape, ExitAdjustPriorityMode, new HotKeyOptions { Description = "Exit Adjust Priorities" })
                .Add(ModCode.None, Code.Enter, MovePriorityAsync, new HotKeyOptions { Description = "Adjust Priorities" })
                .Add(ModCode.Alt, Code.F5, RefreshAsync, new HotKeyOptions { Description = "Refresh" });
        }

        if (_isInitialized)
        {
            return;
        }

        if (TeamSelection != null)
        {
            await SetDefaultWithFilterAsync();
            await SetDefaultActionItemFilterAsync();
            await TeamSelection.SetDefaultTeamsAsync(LocalStorage);
            StateHasChanged();
            _isInitialized = true;
        }
    }

    private async Task RefreshAsync()
    {
        if (_sprints == null)
        {
            throw new InvalidOperationException("Sprint Teams has not been initialized");
        }

        InLoading = InLoadingCssClass;
        if (PriorityAdjustment.ShowExitModeClassName)
        {
            PriorityAdjustment.ToggleMode();
        }
        StateHasChanged();

        var workItems = (await SprintBoardService.GetWorkItemsAsync(_sprints)).ToArray();
        SetWorkItems(workItems);
        StateHasChanged();

        await SprintBoardService.GetPullRequestsAsync(workItems);
        StateHasChanged();
        ResetWorkItemCounts();

        await RefreshRunningWorkItemIdsAsync();

        InLoading = CssClass.None;
    }

    private IReadOnlyCollection<int> _runningWorkItemIds = [];

    private async Task RefreshRunningWorkItemIdsAsync()
    {
        try
        {
            _runningWorkItemIds = await TimerService.GetActivelyTimedWorkItemIdsAsync();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            AlertService.BroadcastAlert(ex);
        }
    }

    private void SetWorkItems(IEnumerable<WorkItem> workItems) =>
        SetWorkItems(workItems as WorkItem[] ?? workItems.ToArray());
    private void SetWorkItems(WorkItem[] workItems)
    {
        PriorityAdjustment = new PriorityAdjustmentViewModel(workItems, AlertService);
        InLoadingWorkItem = workItems.ToDictionary(wi => wi, _ => CssClass.None);
        _workItems = workItems;
        ResetWorkItemCounts();
    }

    private async Task RefreshAsync(WorkItem workItem)
    {
        if (_workItems == null)
        {
            return;
        }

        InLoadingWorkItem[workItem] = InLoadingCssClass;
        StateHasChanged();

        await RefreshRunningWorkItemIdsAsync();

        try
        {
            var workItems = _workItems.ToList();
            await SprintBoardService.RefreshWorkItemAsync(workItems, workItem);
            SetWorkItems(workItems);
        }
        catch (Exception ex)
        {
            AlertService.BroadcastAlert(ex);
        }
        finally
        {
            InLoadingWorkItem[workItem] = CssClass.None;
        }
    }

    private static readonly CssClass InLoadingCssClass = new("in-loading");
    private CssClass InLoading { get; set; } = InLoadingCssClass;
    private Dictionary<WorkItem,CssClass> InLoadingWorkItem { get; set; } = [];

    private async Task OpenWorkItemAsync(WorkItem workItem)
    {
        if (PriorityAdjustment.ShowExitModeClassName)
        {
            return;
        }

        await JsRuntime.InvokeVoidAsync("open", workItem.Url, "_blank");
    }

    private Dictionary<WorkItem, VisibleCssClass> _workItemFilter = [];

    #region With Filter

    private const string DefaultWithFilterStorageKey = "SprintBoard.With";
    private const string WithQueryParamName = "with";

    public required PersonFilter WithPersonFilter { get; set; }

    private async Task SetDefaultWithFilterAsync()
    {
        var hasPersonOnUrl = new Url(NavigationManager.Uri).QueryParams.Any(qp => qp.Name == WithQueryParamName);
        if (hasPersonOnUrl)
        {
            return;
        }

        var filterValue = await LocalStorage.GetItemAsync<string>(DefaultWithFilterStorageKey) ?? "all";
        WithPersonFilter.FilterKey = filterValue;
    }

    private async Task WithFilterChangedAsync()
    {
        ResetWorkItemCounts();
        ResetWithOnUrl();
        await StoreWithFilterAsync();
    }

    private void ResetWithOnUrl()
    {
        var filterValue = WithPersonFilter.FilterKey;

        var url = NavigationManager.Uri
            .RemoveQueryParam(WithQueryParamName)
            .AppendQueryParam(WithQueryParamName, filterValue);

        NavigationManager.NavigateTo(url, forceLoad: false);
    }

    private async Task StoreWithFilterAsync()
    {
        if (LocalStorage == null)
        {
            return;
        }

        var filterValue = WithPersonFilter.FilterKey;
        await LocalStorage.SetItemAsync(DefaultWithFilterStorageKey, filterValue);
    }


    private void WithFilterInitializeFromUrl()
    {
        var parameters = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == WithQueryParamName)
            .ToArray();
        if (parameters.None())
        {
            return;
        }
        var filterValue = parameters.First().Value.ToString() ?? "all";
        WithPersonFilter.FilterKey = filterValue;
    }

    #endregion With Filter

    #region ActionItem (On) Filter

    private const string DefaultActionItemFilterStorageKey = "SprintBoard.ActionItem";
    private const string ActionItemQueryParamName = "on";

    public required PersonFilter ActionItemPersonFilter { get; set; }

    private async Task SetDefaultActionItemFilterAsync()
    {
        var hasPersonOnUrl = new Url(NavigationManager.Uri).QueryParams.Any(qp => qp.Name == ActionItemQueryParamName);
        if (hasPersonOnUrl)
        {
            return;
        }

        var filterValue = await LocalStorage.GetItemAsync<string>(DefaultActionItemFilterStorageKey) ?? "all";
        ActionItemPersonFilter.FilterKey = filterValue;
    }

    private async Task ActionItemFilterChangedAsync()
    {
        ResetWorkItemCounts();
        ResetActionItemOnUrl();
        await StoreActionItemFilterAsync();
    }

    private IEnumerable<Person> ActionItemPeople
    {
        get
        {
            if (_workItems == null)
            {
                return [];
            }

            var actionItems = _workItems.SelectMany(wi => wi.ActionItems);
            var personPriorities = actionItems.SelectMany(actionItem => actionItem.On);
            return personPriorities.Select(x => x.Person).Distinct();
        }
    }

    private void ResetActionItemOnUrl()
    {
        var filterValue = ActionItemPersonFilter.FilterKey;

        var url = NavigationManager.Uri
            .RemoveQueryParam(ActionItemQueryParamName)
            .AppendQueryParam(ActionItemQueryParamName, filterValue);

        NavigationManager.NavigateTo(url, forceLoad: false);
    }

    private async Task StoreActionItemFilterAsync()
    {
        if (LocalStorage == null)
        {
            return;
        }

        var filterValue = ActionItemPersonFilter.FilterKey;
        await LocalStorage.SetItemAsync(DefaultActionItemFilterStorageKey, filterValue);
    }


    private void ActionItemFilterInitializeFromUrl()
    {
        var parameters = new Url(NavigationManager.Uri).QueryParams
            .Where(qp => qp.Name == ActionItemQueryParamName)
            .ToArray();
        if (parameters.None())
        {
            return;
        }
        var filterValue = parameters.First().Value.ToString() ?? "all";
        ActionItemPersonFilter.FilterKey = filterValue;
    }

    private static IEnumerable<ActionItem> OrderActionItems(WorkItem workItem)
    {
        return workItem.ActionItems
            .OrderBy(actionItem =>
                actionItem switch
                {
                    WorkItemActionItem => 0,
                    PullRequestActionItem => 1,
                    _ => 2
                })
            .ThenBy(actionItem => actionItem is WorkItemActionItem workItemAction ? workItemAction.WorkItem.Type : WorkItemType.Task)
            .ThenByDescending(actionItem => actionItem is WorkItemActionItem workItemAction ? workItemAction.WorkItem.State : ScrumState.Unknown)
            .ThenBy(actionItem => actionItem is TaskActionItem task ? task.WorkItem.Id : int.MaxValue)
            .ThenBy(actionItem => actionItem is PullRequestActionItem pr ? pr.PullRequest.Id : int.MaxValue)
            .ThenBy(actionItem => actionItem is PullRequestActionItem pr ? (pr.PullRequest.CreatedBy.IsIn(pr.On.Select(x => x.Person)) ? 0 : 1) : int.MaxValue)
            ;
    }

    #endregion On (ActionItem) Filter

    #region Team Selection

    private TeamSelectionViewModel? TeamSelection { get; set; }

    #endregion Team Selection

    #region Work Item Count

    private int WorkItemActiveCount { get; set; }
    private int WorkInProgressCount { get; set; }
    private int WorkItemDoneCount { get; set; }

    private void ResetWorkItemCounts(object? sender, EventArgs eventArgs) => ResetWorkItemCounts();

    private void ResetWorkItemCounts()
    {
        var selectedTeamIds = TeamSelection?.SelectedTeamIds ?? [];

        var teamWorkItems =
            _workItems?.Where(IsVisible)
            .ToArray()
            ?? [];
        WorkItemActiveCount = teamWorkItems.Count(wi => wi.State != ScrumState.Done);
        WorkInProgressCount = teamWorkItems.Count(IsInProgress);
        WorkItemDoneCount = teamWorkItems.Length - WorkItemActiveCount;

        _workItemFilter = _workItems?.ToDictionary(
            wi => wi,
            wi => (VisibleCssClass) IsVisible(wi)
        ) ?? [];

        return;

        bool IsVisible(WorkItem workItem)
        {
            return (workItem.Sprint?.TeamId.IsIn(selectedTeamIds) ?? false)
                && (WithPersonFilter.CurrentPerson == Person.Anyone || WithPersonFilter.CurrentPerson.IsIn(workItem.WithPeople))
                && (ActionItemPersonFilter.CurrentPerson == Person.Anyone || ActionItemPersonFilter.CurrentPerson.IsIn(workItem.ActionItems.SelectMany(actionItem => actionItem.On).Select(x => x.Person)));
        }
    }

    private static bool IsInProgress(WorkItem wi)
    {
        return wi.State != ScrumState.Done
               && wi.Children.Any(task => task.State.IsIn(ScrumState.InProgress, ScrumState.Done));
    }

    #endregion Work Item Count

    #region Adjust Priority

    private PriorityAdjustmentViewModel PriorityAdjustment { get; set; } = null!;
    
    private void EnterAdjustPriorityMode()
    {
        if (PriorityAdjustment.ShowEnterModeClassName)
        {
            PriorityAdjustment.ToggleMode();
        }
    }

    private void ExitAdjustPriorityMode()
    {
        if (PriorityAdjustment.ShowExitModeClassName)
        {
            PriorityAdjustment.ToggleMode();
        }
    }

    private async Task MovePriorityAsync()
    {
        if (PriorityAdjustment.ShowEnterModeClassName)
        {
            return;
        }

        AlertService.ClearAlert();

        if (PriorityAdjustment.SelectedWorkItemsCount == 0)
        {
            AlertService.BroadcastAlert("No work items selected.  Select work items to move (have their priority changed).");
            return;
        }

        try
        {
            await SprintBoardService.ReorderWorkItemsAsync(PriorityAdjustment.Request);
            PriorityAdjustment.ToggleMode();
        }
        catch (Exception ex)
        {
            var logger = LoggerFactory.CreateLogger<SprintBoards>();
            logger.LogError(ex, "Error moving work items priority.  {Request}", PriorityAdjustment.Request);

            AlertService.BroadcastAlert(ex);
        }
    }

    #endregion Adjust Priority
}

/// <summary>
/// Backing model for the team selection on the sprint board view/razor page.
/// </summary>
/// <remarks>
/// <para>
/// If the URL explicitly has team names on it, then always select those values.
/// This allows users to create bookmark for their team(s).
/// </para>
/// <para>
/// If no teams are on the URL (i.e. https://satori.nexus/Sprints) then the page should default to the team(s) the user selected on their last visit.
/// Those are stored in Local Storage on the client's web browser.
/// Note that local storage is only available after the page has loaded.  Only then can we have access to the LocalStorage service.
/// </para>
/// <para>
/// If neither are selected, then the page should default to all teams.
/// </para>
/// </remarks>
internal class TeamSelectionViewModel
{
    public event EventHandler<EventArgs>? SelectedTeamChanged;

    private readonly Sprint[] _sprints;
    private NavigationManager NavigationManager { get; }
    private ILocalStorageService? LocalStorage { get; set; }

    public TeamSelectionViewModel(Sprint[] sprints, NavigationManager navigationManager)
    {
        _sprints = sprints;
        NavigationManager = navigationManager;

        var teamIds = GetTeamIdsFromUrl(sprints) ?? sprints.Select(sprint => sprint.TeamId).ToArray();
        TeamSelectedClassName =
            sprints.ToDictionary(
                sprint => sprint.TeamId,
                sprint => sprint.TeamId.IsIn(teamIds) ? FilterSelectionCssClass.Selected : FilterSelectionCssClass.Hidden);
    }

    #region Selected Teams

    public Dictionary<Guid, FilterSelectionCssClass> TeamSelectedClassName { get; }

    public async Task SelectTeamAsync(Guid teamId)
    {
        if (TeamSelectedClassName.TryGetValue(teamId, out var value))
        {
            TeamSelectedClassName[teamId] = value == FilterSelectionCssClass.Hidden ? FilterSelectionCssClass.Selected : FilterSelectionCssClass.Hidden;
        }
        else
        {
            TeamSelectedClassName[teamId] = FilterSelectionCssClass.Selected;
        }

        await StoreDefaultTeamIdsAsync();
        ResetUrl();
        OnSelectedTeamChanged();
    }

    private void OnSelectedTeamChanged()
    {
        SelectedTeamChanged?.Invoke(this, EventArgs.Empty);
    }

    public Guid[] SelectedTeamIds =>
        TeamSelectedClassName
            .Where(kvp => kvp.Value == FilterSelectionCssClass.Selected)
            .Select(kvp => kvp.Key)
            .ToArray();

    #endregion Selected Teams

    #region Url

    private const string TeamsQueryParameter = "team";

    private Guid[]? GetTeamIdsFromUrl(Sprint[] sprints)
    {
        var teamIds = new Url(NavigationManager.Uri)
            .QueryParams
            .Where(qp => qp.Name == TeamsQueryParameter)
            .Select(qp => qp.Value.ToString())
            .Join(sprints, x => x, sprint => sprint.TeamName, (_, sprint) => sprint.TeamId)
            .ToArray();

        return teamIds.None() ? null : teamIds;
    }

    private bool HasTeamsOnUrl()
    {
        return new Url(NavigationManager.Uri).QueryParams.Any(qp => qp.Name == TeamsQueryParameter);
    }

    private void ResetUrl()
    {
        var selectedTeamNames =
            _sprints.Where(sprint => sprint.TeamId.IsIn(SelectedTeamIds))
                .Select(sprint => sprint.TeamName);

        var url = NavigationManager.Uri
            .RemoveQueryParam(TeamsQueryParameter)
            .AppendQueryParam(TeamsQueryParameter, selectedTeamNames);

        NavigationManager.NavigateTo(url, forceLoad: false);
    }

    #endregion Url

    #region Local Storage

    private const string DefaultTeamIdsStorageKey = "SprintBoard.DefaultTeamIds";

    public async Task SetDefaultTeamsAsync(ILocalStorageService localStorage)
    {
        LocalStorage = localStorage;
        if (!HasTeamsOnUrl())
        {
            var teamIds = await LocalStorage.GetItemAsync<Guid[]>(DefaultTeamIdsStorageKey) ?? [];
            if (teamIds.Length > 0)
            {
                foreach (var teamId in TeamSelectedClassName.Keys)
                {
                    TeamSelectedClassName[teamId] = teamId.IsIn(teamIds)
                        ? FilterSelectionCssClass.Selected
                        : FilterSelectionCssClass.Hidden;
                }
                OnSelectedTeamChanged();
            }
        }
    }

    private async Task StoreDefaultTeamIdsAsync()
    {
        if (LocalStorage == null)
        {
            return;
        }

        await LocalStorage.SetItemAsync(DefaultTeamIdsStorageKey, SelectedTeamIds);
    }

    #endregion Local Storage
}

internal class FilterSelectionCssClass : CssClass
{
    private FilterSelectionCssClass(string className) : base(className)
    {
    }

    public static readonly FilterSelectionCssClass Selected = new("filter-selected");
    public static readonly FilterSelectionCssClass Hidden = new("filter-hidden");

    public FilterSelectionCssClass Not => this == Selected ? Hidden : Selected;

    public static implicit operator bool(FilterSelectionCssClass cssClass) => cssClass == Selected;
    public static implicit operator FilterSelectionCssClass(bool value) => value ? Selected : Hidden;

}

/// <summary>
/// Backing model for the priority adjustment on the sprint board view/razor page.
/// </summary>
/// <para>
/// Stores whether the page is in "Adjust Priority" mode <see cref="ShowEnterModeClassName"/>.
/// Stores the <see cref="SelectedWorkItems"/> that will be moved to a new position.
/// Stores the <see cref="Target"/> work item for the new position and whether it is above or below (<see cref="TargetRelation"/>).
/// </para>
internal class PriorityAdjustmentViewModel
{
    private readonly WorkItem[] _workItems;
    private readonly IAlertService _alertService;

    internal PriorityAdjustmentViewModel(WorkItem[] workItems, IAlertService alertService)
    {
        _workItems = workItems;
        _alertService = alertService;

        ClearSelectedWorkItems();
        ShowSelectWorkItemClassName = workItems.ToDictionary(wi => wi.Id, _ => VisibleCssClass.Hidden);
        ShowDeselectWorkItemClassName = workItems.ToDictionary(wi => wi.Id, _ => VisibleCssClass.Hidden);
        WorkItemSelectedClassName = workItems.ToDictionary(wi => wi.Id, _ => RowSelectedCssClass.Deselected);
    }

    public ReorderRequest Request => new(
        _workItems,
        SelectedWorkItems.ToArray(),
        TargetRelation,
        Target);


    #region Current Mode

    public VisibleCssClass ShowEnterModeClassName { get; private set; } = VisibleCssClass.Visible;
    public VisibleCssClass ShowExitModeClassName { get; private set; } = VisibleCssClass.Hidden;

    public void ToggleMode()
    {
        ShowEnterModeClassName = !ShowEnterModeClassName;
        ShowExitModeClassName = !ShowExitModeClassName;

        ClearSelectedWorkItems();
        Target = null;
        _alertService.ClearAlert();
    }

    #endregion Current Mode

    #region Selected Work Items

    private List<WorkItem> _selectedWorkItems = [];
    public IReadOnlyCollection<WorkItem> SelectedWorkItems => _selectedWorkItems;

    public int SelectedWorkItemsCount { get; private set; }

    public Dictionary<int, VisibleCssClass> ShowSelectWorkItemClassName { get; private set; }
    public Dictionary<int, VisibleCssClass> ShowDeselectWorkItemClassName { get; private set; }
    public Dictionary<int, RowSelectedCssClass> WorkItemSelectedClassName { get; private set; }

    private void ClearSelectedWorkItems()
    {
        _selectedWorkItems = [];
        SelectedWorkItemsCount = SelectedWorkItems.Count;

        var showSelectWorkItemButtonClassName =
            ShowEnterModeClassName == VisibleCssClass.Visible ? VisibleCssClass.Hidden : VisibleCssClass.Visible;

        ShowSelectWorkItemClassName = _workItems.ToDictionary(wi => wi.Id, _ => showSelectWorkItemButtonClassName);
        ShowDeselectWorkItemClassName = _workItems.ToDictionary(wi => wi.Id, _ => VisibleCssClass.Hidden);
        WorkItemSelectedClassName = _workItems.ToDictionary(wi => wi.Id, _ => RowSelectedCssClass.Deselected);
    }

    public void AddSelectedWorkItem(WorkItem workItem)
    {
        _selectedWorkItems.Add(workItem);
        SelectedWorkItemsCount = SelectedWorkItems.Count;

        ShowSelectWorkItemClassName[workItem.Id] = VisibleCssClass.Hidden;
        ShowDeselectWorkItemClassName[workItem.Id] = VisibleCssClass.Visible;
        WorkItemSelectedClassName[workItem.Id] = RowSelectedCssClass.Selected;

        _alertService.ClearAlert();
    }

    public void RemoveSelectedWorkItem(WorkItem workItem)
    {
        _selectedWorkItems.Remove(workItem);
        SelectedWorkItemsCount = SelectedWorkItems.Count;

        ShowSelectWorkItemClassName[workItem.Id] = VisibleCssClass.Visible;
        ShowDeselectWorkItemClassName[workItem.Id] = VisibleCssClass.Hidden;
        WorkItemSelectedClassName[workItem.Id] = RowSelectedCssClass.Deselected;

        _alertService.ClearAlert();
    }

    internal class RowSelectedCssClass : CssClass
    {
        private RowSelectedCssClass(string className) : base(className)
        {
        }

        public static readonly RowSelectedCssClass Selected = new("selected-for-priority-adjust");
        public static readonly RowSelectedCssClass Deselected = new(string.Empty);
    }

    #endregion Selected Work Items

    #region Move Above/Below Target

    private RelativePosition _targetRelation = RelativePosition.Below;

    public RelativePosition TargetRelation
    {
        get => _targetRelation;
        private set
        {
            _targetRelation = value;
            SetMoveToLabel();
        }
    }

    private WorkItem? _target;

    public WorkItem? Target
    {
        get => _target;
        private set
        {
            _target = value;
            SetMoveToLabel();
        }
    }

    private void SetMoveToLabel()
    {
        MoveToLabel = Target == null
            ? TargetRelation == RelativePosition.Below ? "Bottom" : "Top"
            : TargetRelation == RelativePosition.Below ? "Below" : "Above";

        _alertService.ClearAlert();
    }

    public string MoveToLabel { get; private set; } = "Below";


    public void ToggleMoveBelow()
    {
        TargetRelation = TargetRelation == RelativePosition.Below ? RelativePosition.Above : RelativePosition.Below;
    }

    public void SetMoveTo(WorkItem workItem, MouseEventArgs e)
    {
        if (workItem.IsNotIn(SelectedWorkItems))
        {
            Target = workItem;
            TargetRelation = e.OffsetY <= 30 ? RelativePosition.Above : RelativePosition.Below;
        }
    }

    public void ClearMoveTo()
    {
        Target = null;
    }


    #endregion Target
}
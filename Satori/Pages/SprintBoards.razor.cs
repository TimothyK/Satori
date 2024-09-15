using Blazored.LocalStorage;
using CodeMonkeyProjectiles.Linq;
using Flurl;
using Microsoft.AspNetCore.Components;
using Satori.AppServices.ViewModels.Sprints;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Utilities;

namespace Satori.Pages
{
    /// <summary>
    /// Backing model for the team selection on the sprint board view/razor page.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the URL explicitly has team names on it, then always select those values.
    /// This allows users to create bookmark for their team(s).
    /// </para>
    /// <para>
    /// If no teams are on the URL (i.e. http://satori/Sprints) then the page should default to the team(s) the user selected on their last visit.
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
                    sprint => sprint.TeamId.IsIn(teamIds) ? TeamSelectionCssClass.Selected : TeamSelectionCssClass.Hidden);
        }

        #region Selected Teams

        public Dictionary<Guid, TeamSelectionCssClass> TeamSelectedClassName { get; }

        public void SelectTeam(Guid teamId)
        {
            if (TeamSelectedClassName.TryGetValue(teamId, out var value))
            {
                TeamSelectedClassName[teamId] = value == TeamSelectionCssClass.Hidden ? TeamSelectionCssClass.Selected : TeamSelectionCssClass.Hidden;
            }
            else
            {
                TeamSelectedClassName[teamId] = TeamSelectionCssClass.Selected;
            }

            StoreDefaultTeamIds();
            ResetUrl();
            OnSelectedTeamChanged();
        }

        private void OnSelectedTeamChanged()
        {
            SelectedTeamChanged?.Invoke(this, EventArgs.Empty);
        }

        public Guid[] SelectedTeamIds =>
            TeamSelectedClassName
                .Where(kvp => kvp.Value == TeamSelectionCssClass.Selected)
                .Select(kvp => kvp.Key)
                .ToArray();

        internal class TeamSelectionCssClass : CssClass
        {
            private TeamSelectionCssClass(string className) : base(className)
            {
            }

            public static readonly TeamSelectionCssClass Selected = new("team-selected");
            public static readonly TeamSelectionCssClass Hidden = new("team-hidden");
        }

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
                            ? TeamSelectionCssClass.Selected
                            : TeamSelectionCssClass.Hidden;
                    }
                    OnSelectedTeamChanged();
                }
            }
        }

        private void StoreDefaultTeamIds()
        {
            if (LocalStorage == null)
            {
                return;
            }

#pragma warning disable CA2012
            Task.Run(() => LocalStorage.SetItemAsync(DefaultTeamIdsStorageKey, SelectedTeamIds)).GetAwaiter().GetResult();
#pragma warning restore CA2012
        }

        #endregion Local Storage
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

        internal PriorityAdjustmentViewModel(WorkItem[] workItems)
        {
            _workItems = workItems;

            ClearSelectedWorkItems();
            ShowSelectWorkItemClassName = workItems.ToDictionary(wi => wi.Id, _ => VisibleCssClass.Hidden);
            ShowDeselectWorkItemClassName = workItems.ToDictionary(wi => wi.Id, _ => VisibleCssClass.Hidden);
            WorkItemSelectedClassName = workItems.ToDictionary(wi => wi.Id, _ => RowSelectedCssClass.Deselected);
        }

        public ReorderRequest Request => new(
            _workItems, 
            SelectedWorkItems.Select(wi => wi.Id).ToArray(), 
            TargetRelation == RelativePosition.Below, 
            Target);

        #region Alert Message

        public string HideAlertClassName { get; private set; } = "d-none";
        public AlertTypeCssClass AlertTypeClassName { get; private set; } = AlertTypeCssClass.Error;

        public string AlertContent { get; private set; } = string.Empty;

        public void ClearAlert()
        {
            HideAlertClassName = "d-none";
            AlertTypeClassName = AlertTypeCssClass.Error;
        }

        public void ShowAlert(string message, AlertTypeCssClass type)
        {
            AlertContent = message;
            AlertTypeClassName = type;
            HideAlertClassName = string.Empty;
        }

        internal class AlertTypeCssClass : CssClass
        {
            private AlertTypeCssClass(string className) : base(className)
            {
            }

            public static readonly AlertTypeCssClass Error = new("alert-danger");
            public static readonly AlertTypeCssClass Warning = new("alert-warning");
        }

        #endregion Alert Message

        #region Current Mode

        public VisibleCssClass ShowEnterModeClassName { get; private set; } = VisibleCssClass.Visible;
        public VisibleCssClass ShowExitModeClassName { get; private set; } = VisibleCssClass.Hidden;

        public void ToggleMode()
        {
            ShowEnterModeClassName = !ShowEnterModeClassName;
            ShowExitModeClassName = !ShowExitModeClassName;

            ClearSelectedWorkItems();
            Target = null;
            HideAlertClassName = "d-none";
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

            ClearAlert();
        }

        public void AddSelectedWorkItem(WorkItem workItem)
        {
            _selectedWorkItems.Add(workItem);
            SelectedWorkItemsCount = SelectedWorkItems.Count;

            ShowSelectWorkItemClassName[workItem.Id] = VisibleCssClass.Hidden;
            ShowDeselectWorkItemClassName[workItem.Id] = VisibleCssClass.Visible;
            WorkItemSelectedClassName[workItem.Id] = RowSelectedCssClass.Selected;

            ClearAlert();
        }

        public void RemoveSelectedWorkItem(WorkItem workItem)
        {
            _selectedWorkItems.Remove(workItem);
            SelectedWorkItemsCount = SelectedWorkItems.Count;

            ShowSelectWorkItemClassName[workItem.Id] = VisibleCssClass.Visible;
            ShowDeselectWorkItemClassName[workItem.Id] = VisibleCssClass.Hidden;
            WorkItemSelectedClassName[workItem.Id] = RowSelectedCssClass.Deselected;

            ClearAlert();
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

            ClearAlert();
        }

        public string MoveToLabel { get; private set; } = "Below";


        public void ToggleMoveBelow()
        {
            TargetRelation = TargetRelation == RelativePosition.Below ? RelativePosition.Above : RelativePosition.Below;
        }

        public void SetMoveTo(WorkItem workItem)
        {
            if (workItem.IsNotIn(SelectedWorkItems))
            {
                Target = workItem;
            }
        }

        public void ClearMoveTo()
        {
            Target = null;
        }

        public enum RelativePosition
        {
            Above,
            Below
        }

        #endregion Target
    }
}

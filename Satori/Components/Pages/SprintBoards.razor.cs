using CodeMonkeyProjectiles.Linq;
using Satori.AppServices.ViewModels.WorkItems;
using Satori.Utilities;

namespace Satori.Components.Pages
{
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

        #region Current Mode

        public VisibleCssClass ShowEnterModeClassName { get; private set; } = VisibleCssClass.Visible;
        public VisibleCssClass ShowExitModeClassName { get; private set; } = VisibleCssClass.Hidden;

        public void ToggleMode()
        {
            ShowEnterModeClassName = !ShowEnterModeClassName;
            ShowExitModeClassName = !ShowExitModeClassName;

            ClearSelectedWorkItems();
            Target = null;
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
        }

        public void RemoveSelectedWorkItem(WorkItem workItem)
        {
            _selectedWorkItems.Remove(workItem);
            SelectedWorkItemsCount = SelectedWorkItems.Count;

            ShowSelectWorkItemClassName[workItem.Id] = VisibleCssClass.Visible;
            ShowDeselectWorkItemClassName[workItem.Id] = VisibleCssClass.Hidden;
            WorkItemSelectedClassName[workItem.Id] = RowSelectedCssClass.Deselected;
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace XrEditor
{
    /// <summary>
    /// ListBox used to render a ListTreeView.
    ///
    /// The model (ListTreeNodeView.IsSelected) is the only source of truth.
    /// - User -> model: OnSelectionChanged pushes Added/Removed items into the model,
    ///   only when the change is a real user change (not caused by rows being added/removed/reset
    ///   and not caused by our own sync).
    /// - Model -> ListBox: on ListTreeView.SelectionStateChanged a (coalesced, deferred) full sync
    ///   sets SelectedItems from the model. SelectedItems is item based, so it works with virtualization
    ///   (unlike binding ListBoxItem.IsSelected, which only exists for realized containers).
    /// </summary>
    public class ListTreeListBox : ListBox
    {
        private ListTreeView? _tree;
        private bool _isSyncing;
        private bool _syncPending;

        public ListTreeListBox()
        {
            SelectionMode = SelectionMode.Extended;

            Loaded += (_, _) => Attach(DataContext as ListTreeView);
            Unloaded += (_, _) => Attach(null);
            DataContextChanged += (_, e) =>
            {
                if (IsLoaded)
                    Attach(e.NewValue as ListTreeView);
            };
        }

        private void Attach(ListTreeView? tree)
        {
            if (_tree == tree)
                return;

            if (_tree != null)
                _tree.SelectionStateChanged -= ScheduleSync;

            _tree = tree;

            if (_tree != null)
            {
                _tree.SelectionStateChanged += ScheduleSync;
                ScheduleSync();
            }
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_tree == null || _isSyncing)
                return;

            if (_tree.IsChangingItems)
            {
                // Rows added / removed / reset: WPF dropped or re-validated its selection.
                // Not a user action, the model wins.
                ScheduleSync();
                return;
            }

            foreach (var item in e.RemovedItems)
            {
                // A removed row (collapsed parent) keeps its model selection
                if (item is ListTreeNodeView node && node.IsVisible)
                    node.IsSelected = false;
            }

            foreach (var item in e.AddedItems)
            {
                if (item is ListTreeNodeView node)
                    node.IsSelected = true;
            }

            // Handlers of the model (OutlinePanel / SelectionManager) may have changed other nodes
            ScheduleSync();
        }

        private void ScheduleSync()
        {
            if (_syncPending)
                return;

            _syncPending = true;

            // Deferred: never touch the Selector while it is inside its own SelectionChange.
            // DataBind priority runs before Render, so there is no visible flicker.
            Dispatcher.InvokeAsync(SyncFromModel, DispatcherPriority.DataBind);
        }

        private void SyncFromModel()
        {
            _syncPending = false;

            if (_tree == null)
                return;

            var desired = _tree.SelectedItems.Where(a => a.IsVisible).ToList();

            if (desired.Count == SelectedItems.Count &&
                SelectedItems.Cast<object>().All(a => a is ListTreeNodeView n && n.IsSelected && n.IsVisible))
                return;

            _isSyncing = true;
            try
            {
                SetSelectedItems(desired);
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}

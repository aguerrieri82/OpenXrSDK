using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace XrEditor
{
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
                ScheduleSync();
                return;
            }

            foreach (var item in e.RemovedItems)
            {
                if (item is ListTreeNodeView node && node.IsVisible)
                    node.IsSelected = false;
            }

            foreach (var item in e.AddedItems)
            {
                if (item is ListTreeNodeView node)
                    node.IsSelected = true;
            }

            ScheduleSync();
        }

        private void ScheduleSync()
        {
            if (_syncPending)
                return;

            _syncPending = true;

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

using System.Collections.ObjectModel;
using XrEditor.Abstraction;

namespace XrEditor
{
    /*
     * Invariants
     * ----------
     * 1) ListTreeView.Items contains EXACTLY the visible rows (a node is in Items only if
     *    all its ancestors are expanded and the root is in Items). There are no hidden rows:
     *    hidden rows would be part of Shift-range selections and keyboard navigation.
     * 2) Every node in Items has _index == its position in Items, every other node has _index == -1.
     *    Indexes are rebuilt immediately after every mutation (InsertNodes / RemoveNodes).
     * 3) ListTreeNodeView.IsSelected is the ONLY source of truth for the selection.
     *    A view is a POSITION in the tree: the same model object can have many views
     *    (e.g. a material shared by two meshes). Views are never reused: when a view leaves
     *    the tree it is discarded and Removed is raised.
     *    The WPF ListBox is never bound to it: ListTreeListBox pushes user changes into the model
     *    and re-syncs its SelectedItems from the model (see XrEditor.App/Components/ListTreeListBox.cs).
     */
    public class ListTreeNodeView : BaseView, IEditorUIElementHost
    {
        protected ListTreeNodeView? _parent;
        protected ListTreeView _host;
        protected internal int _index;
        protected internal List<ListTreeNodeView>? _children;
        protected int _level;
        protected bool _childrenLoaded;
        protected bool _isExpanded;
        protected object? _header;
        protected bool _isSelected;
        protected bool _isLeaf;
        protected IEditorUIElement? _uiElement;

        public ListTreeNodeView(ListTreeView host, ListTreeNodeView? parent)
        {
            _host = host;
            _parent = parent;
            _index = -1;
            ToggleCommand = new Command(Toggle);
        }

        /// <summary>Collapses and discards the children; they will be loaded again on next expand.</summary>
        public void Unload()
        {
            IsExpanded = false;
            Clear();
            _childrenLoaded = false;
        }

        public void AddChild(ListTreeNodeView child)
        {
            if (_children != null && _children.Contains(child))
                return;

            // Safety: a view can't be shown twice or be owned by two parents.
            child.Detach();

            _children ??= [];

            child._parent = this;
            child.SetLevel(_level + 1);

            if (_isExpanded && IsVisible)
            {
                var insertIndex = _index + 1 + CountVisibleDescendants();
                _children.Add(child);
                _host.InsertNodes(insertIndex, child.SelfAndVisibleDescendants().ToList());
            }
            else
                _children.Add(child);

            if (child._isSelected)
                _host._selectedItems.Add(child);

            IsLeaf = false;
        }

        public void RemoveChild(ListTreeNodeView child)
        {
            if (child._parent == this)
                child.Remove();
            else
                _children?.Remove(child);
        }

        /// <summary>
        /// Discards all the child views. The selection of the underlying objects is NOT changed
        /// (used by Refresh / Unload: the objects still exist, only the views are rebuilt).
        /// </summary>
        public void Clear()
        {
            if (_children == null)
                return;

            _host.BeginUpdate(_children.Count > ListTreeView.MassiveThreshold);
            try
            {
                for (var i = _children.Count - 1; i >= 0; i--)
                {
                    var child = _children[i];
                    if (child._parent == this)
                        child.Discard();
                }

                _children.Clear();
            }
            finally
            {
                _host.EndUpdate();
            }
        }

        /// <summary>
        /// The object was removed: deselect it (and its descendants) and discard the view.
        /// </summary>
        public void Remove()
        {
            foreach (var item in DescendantsOrSelf().Where(a => a._isSelected).ToList())
                item.IsSelected = false;

            Discard();
        }

        /// <summary>
        /// Removes the view (and its child views) from the tree without notifying selection changes,
        /// then raises Removed for each discarded view. The view must not be used anymore.
        /// </summary>
        internal void Discard()
        {
            IsExpanded = false;

            Clear();

            // only drop the view from the host selection, the object selection is not changed
            SetSelectedCore(false, notifySelectionChanged: false);

            Detach();

            Removed?.Invoke(this);
        }

        /// <summary>
        /// Removes the node (and its visible descendants) from the rows and from its parent,
        /// without touching selection / expansion state.
        /// </summary>
        internal void Detach()
        {
            if (IsVisible)
                _host.RemoveNodes(_index, 1 + CountVisibleDescendants());

            _parent?._children?.Remove(this);
            _parent = null;
        }

        public void Refresh()
        {
            Clear();
            LoadChildrenWork();
            _childrenLoaded = true;
            IsLeaf = _children == null || _children.Count == 0;
        }

        protected virtual void LoadChildrenWork()
        {
            if (LoadChildren == null)
                return;

            var result = new List<ListTreeNodeView>();

            LoadChildren(this, result);

            _host.BeginUpdate(result.Count > ListTreeView.MassiveThreshold);
            try
            {
                foreach (var child in result)
                    AddChild(child);
            }
            finally
            {
                _host.EndUpdate();
            }
        }

        public IEnumerable<ListTreeNodeView> DescendantsOrSelf()
        {
            yield return this;

            if (_children == null)
                yield break;

            foreach (var child in _children)
            {
                foreach (var item in child.DescendantsOrSelf())
                    yield return item;
            }
        }

        /// <summary>Descendants that are (or would be, if this node is visible) shown as rows.</summary>
        public IEnumerable<ListTreeNodeView> VisibleDescendants()
        {
            if (!_isExpanded || _children == null)
                yield break;

            foreach (var child in _children)
            {
                yield return child;
                foreach (var item in child.VisibleDescendants())
                    yield return item;
            }
        }

        protected IEnumerable<ListTreeNodeView> SelfAndVisibleDescendants()
        {
            yield return this;
            foreach (var item in VisibleDescendants())
                yield return item;
        }

        protected int CountVisibleDescendants() => VisibleDescendants().Count();

        protected void SetLevel(int level)
        {
            if (_level != level)
            {
                _level = level;
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(Margin));
            }

            if (_children != null)
            {
                foreach (var child in _children)
                    child.SetLevel(level + 1);
            }
        }

        protected void Expand()
        {
            if (!_childrenLoaded)
                Refresh(); // _isExpanded is still false here: children are not inserted as rows

            _isExpanded = true;

            if (IsVisible)
                _host.InsertNodes(_index + 1, VisibleDescendants().ToList());
        }

        protected void Collapse()
        {
            if (IsVisible)
            {
                var count = CountVisibleDescendants(); // must be computed while still expanded
                if (count > 0)
                    _host.RemoveNodes(_index + 1, count);
            }

            _isExpanded = false;
        }

        public void Toggle()
        {
            IsExpanded = !IsExpanded;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetSelectedCore(value, notifySelectionChanged: true);
        }

        internal void SetSelectedCore(bool value, bool notifySelectionChanged)
        {
            if (_isSelected == value)
                return;

            _isSelected = value;

            if (_isSelected)
                _host._selectedItems.Add(this);
            else
                _host._selectedItems.Remove(this);

            OnPropertyChanged(nameof(IsSelected));

            _host.NotifySelectionStateChanged();

            if (notifySelectionChanged)
                SelectionChanged?.Invoke(this);
        }

        /// <summary>The node's own expanded flag (independent from the ancestors).</summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;

                if (value)
                    Expand();
                else
                    Collapse();

                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public object? Header
        {
            get => _header;
            set
            {
                if (_header == value)
                    return;
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        public bool IsLeaf
        {
            get => _isLeaf;
            set
            {
                if (_isLeaf == value)
                    return;
                _isLeaf = value;
                OnPropertyChanged(nameof(IsLeaf));
            }
        }

        public IEditorUIElement? UIElement
        {
            get => _uiElement;
            set
            {
                _uiElement = value;
                if (ScrollOnCreate && value != null)
                {
                    ScrollOnCreate = false;
                    _host.ScrollIntoView(this);
                }
            }
        }

        public bool ScrollOnCreate { get; set; }

        public event Action<ListTreeNodeView, IList<ListTreeNodeView>>? LoadChildren;

        public event Action<ListTreeNodeView>? SelectionChanged;

        /// <summary>Raised when the view is discarded (removed from the tree, it will not be reused).</summary>
        public event Action<ListTreeNodeView>? Removed;

        /// <summary>True if the node is currently a row of ListTreeView.Items.</summary>
        public bool IsVisible => _index >= 0;

        public Command ToggleCommand { get; }

        public IReadOnlyList<ListTreeNodeView> Children => _children ?? [];

        public ListTreeNodeView? Parent
        {
            get => _parent;
            internal set => _parent = value;
        }

        public int Level => _level;

        public double Margin => _level * 16;
    }

    public class ListTreeView : BaseView, IEditorUIElementHost
    {
        internal const int MassiveThreshold = 50;

        protected internal BulkObservableCollection<ListTreeNodeView> _items;
        protected internal HashSet<ListTreeNodeView> _selectedItems;

        private int _updateCount;
        private int _changingItems;
        private ListTreeNodeView? _root;
        private readonly Stack<bool> _massiveStack = new();

        public ListTreeView()
        {
            _items = [];
            _selectedItems = [];
        }

        public void SetRoot(ListTreeNodeView? root)
        {
            var oldRoot = _root;
            _root = root;

            // discard the old views (raises Removed, so the owner can release them)
            if (oldRoot != null && oldRoot != root)
                oldRoot.Discard();

            _changingItems++;
            try
            {
                foreach (var item in _items)
                    item._index = -1;

                _items.Clear();

                if (root != null)
                {
                    root.Detach();
                    _items.Add(root);
                    root._index = 0;
                    InsertNodes(1, root.VisibleDescendants().ToList());
                }
            }
            finally
            {
                _changingItems--;
            }

            NotifySelectionStateChanged();
        }

        /// <summary>
        /// Groups many row changes in a single Reset notification.
        /// Indexes are always kept up to date, only the WPF notification is delayed.
        /// </summary>
        public void BeginUpdate(bool isMassive = false)
        {
            _updateCount++;
            if (isMassive)
                _items.BeginUpdate();
            _massiveStack.Push(isMassive);
        }

        public void EndUpdate()
        {
            var isMassive = _massiveStack.Pop();
            _updateCount--;

            if (isMassive)
            {
                _changingItems++;
                try
                {
                    _items.EndUpdate(); // may raise Reset: ListBox selection changes here are not user changes
                }
                finally
                {
                    _changingItems--;
                }
            }

            if (_updateCount == 0)
                NotifySelectionStateChanged();
        }

        internal void InsertNodes(int index, IList<ListTreeNodeView> nodes)
        {
            if (nodes.Count == 0)
                return;

            var isMassive = nodes.Count > MassiveThreshold;

            _changingItems++;
            try
            {
                if (isMassive)
                    _items.BeginUpdate();
                try
                {
                    for (var i = 0; i < nodes.Count; i++)
                        _items.Insert(index + i, nodes[i]);
                }
                finally
                {
                    if (isMassive)
                        _items.EndUpdate();
                }

                RebuildIndexes(index);
            }
            finally
            {
                _changingItems--;
            }

            if (_updateCount == 0)
                NotifySelectionStateChanged();
        }

        internal void RemoveNodes(int index, int count)
        {
            if (count <= 0)
                return;

            var isMassive = count > MassiveThreshold;

            _changingItems++;
            try
            {
                if (isMassive)
                    _items.BeginUpdate();
                try
                {
                    for (var i = index + count - 1; i >= index; i--)
                    {
                        var node = _items[i];
                        _items.RemoveAt(i);
                        node._index = -1;
                    }
                }
                finally
                {
                    if (isMassive)
                        _items.EndUpdate();
                }

                RebuildIndexes(index);
            }
            finally
            {
                _changingItems--;
            }

            if (_updateCount == 0)
                NotifySelectionStateChanged();
        }

        protected internal void RebuildIndexes(int startIndex = 0)
        {
            for (var i = Math.Max(0, startIndex); i < _items.Count; i++)
                _items[i]._index = i;
        }

        internal void NotifySelectionStateChanged()
        {
            SelectionStateChanged?.Invoke();
        }

        public void ScrollIntoView(ListTreeNodeView node)
        {
            if (UIElement is IEditorUIContainer container)
                container.ScrollToView(node);
            else
            {
                if (node.UIElement == null)
                    node.ScrollOnCreate = true;
                else
                    node.UIElement.ScrollToView();
            }
        }

        /// <summary>
        /// Raised when the selection of any node changes or the rows change.
        /// Used by the UI to re-sync its own selection from the model.
        /// </summary>
        public event Action? SelectionStateChanged;

        /// <summary>True while rows are being inserted / removed / reset.</summary>
        public bool IsChangingItems => _changingItems > 0;

        public IEditorUIElement? UIElement { get; set; }

        public IReadOnlySet<ListTreeNodeView> SelectedItems => _selectedItems;

        public ListTreeNodeView? Root => _root;

        /// <summary>Visible rows. Do not modify directly, use SetRoot / node methods.</summary>
        public ObservableCollection<ListTreeNodeView> Items => _items;
    }
}

﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using XrEditor.Abstraction;
using XrEngine;


namespace XrEditor
{
    public class ListTreeNodeView : BaseView, IEditorUIElementHost
    {
        protected ListTreeNodeView? _parent;
        protected ListTreeView _host;
        protected internal int _index;
        protected internal int _childInsertIndex;
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
            _childInsertIndex = -1;
            _index = -1;
            ToggleCommand = new Command(Toggle);
        }

        public void Unload()
        {
            IsExpanded = false;
            _childrenLoaded = false;
            _children = [];
        }

        public void AddChild(ListTreeNodeView child)
        {
            Debug.Assert(child._children == null || child._children.Count == 0);

            if (_children != null && _children.Contains(child))
                return;

            _host.BeginUpdate(this);
            try
            {
                _children ??= [];

                child._level = _level + 1;
                child._parent = this;

                if (_isExpanded)
                {
                    if (_childInsertIndex == -1)
                        _childInsertIndex = LastDescendant()!._index + 1;
                    child._index = _childInsertIndex++;
                    _host._items.Insert(child._index, child);
                }

                _children.Add(child);

                if (child.IsSelected)
                    _host._selectedItems.Add(child);
            }
            finally
            {
                _host.EndUpdate();
            }
        }


        public void Clear()
        {
            if (_children == null)
                return;

            _host.BeginUpdate(this);

            try
            {
                for (var i = _children.Count - 1; i >= 0; i--)
                {
                    if (_children[i].Parent == null)
                        _children.RemoveAt(i);
                    else
                        _children[i].Remove();
                }

            }
            finally
            {
                _host.EndUpdate();
            }
        }

        public void Remove()
        {
            _host.BeginUpdate(this);

            try
            {
                if (_isExpanded)
                    Collapse();

                if (_isSelected)
                    IsSelected = false;

                Clear();

                if (_index != -1)
                    _host.Items.RemoveAt(_index);

                if (_parent != null)
                    _parent._children!.Remove(this);

                _parent = null;
                _index = -1;
            }
            finally
            {
                _host.EndUpdate();
            }

        }

        public void Refresh()
        {
            _host.BeginUpdate(this);
            try
            {
                Clear();
                LoadChildrenWork();
                _childrenLoaded = true;
                IsLeaf = _children == null || _children.Count == 0;
            }
            finally
            {
                _host.EndUpdate();
            }
        }

        protected virtual void LoadChildrenWork()
        {
            if (LoadChildren == null)
                return;

            var result = new List<ListTreeNodeView>();
            
            LoadChildren(this, result);

            var isMassive = result.Count > 0;

            _host.BeginUpdate(this, isMassive);

            try
            {
                foreach (var child in result)
                    AddChild(child);
            }
            finally
            {
                _host.EndUpdate(isMassive);
            }

        }

        public IEnumerable<ListTreeNodeView> DescendantsOrSelf()
        {
            static IEnumerable<ListTreeNodeView> Visit(ListTreeNodeView node)
            {
                yield return node;

                if (node._children != null)
                {
                    foreach (var child in node._children)
                    {
                        foreach (var innerChild in Visit(child))
                            yield return innerChild;
                    }
                }

            }

            return Visit(this);
        }


        protected ListTreeNodeView? LastDescendant(bool includeSelf = false)
        {
            if (_children == null || _children.Count == 0)
                return includeSelf ? this : null;

            return _children[_children.Count - 1].LastDescendant(true);
        }

        protected void Expand()
        {
            if (!_childrenLoaded)
                Refresh();

            var items = DescendantsOrSelf().Skip(1).ToArray();
            _host._items.InsertRange(_index + 1, items);
            _host.RebuildIndexes(_index);
            /*
            foreach (var item in DescendantsOrSelf())
                item.OnPropertyChanged(nameof(IsVisible));
            */
        }

        protected void Collapse()
        {
            if (_children == null || _children.Count == 0)
                return;

            if (_index == -1)
                return;

            var startIndex = _children[0]._index;
            var endIndex = LastDescendant()!._index;

            _host._items.RemoveRange(startIndex, endIndex - startIndex + 1);

            foreach (var child in DescendantsOrSelf().Skip(1))
            {
                child._index = -1;
                child._childInsertIndex = -1;
            }
            
            _host.RebuildIndexes(_index);

            /*
            foreach (var item in DescendantsOrSelf())
                item.OnPropertyChanged(nameof(IsVisible));
            */
        }

        protected virtual void OnSelectionChanged()
        {
            if (_isSelected)
                _host._selectedItems.Add(this);
            else
                _host._selectedItems.Remove(this);

            SelectionChanged?.Invoke(this);
        }

        public void Toggle()
        {
            IsExpanded = !IsExpanded;
        }


        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                Log.Debug(this, "Sel: {0} ({1})", value, this.Header);

                _isSelected = value;

                OnSelectionChanged();

                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded && (_parent == null || _parent.IsExpanded);
            set
            {
                if (_isExpanded == value)
                    return;

                if (value)
                    Expand();
                else
                    Collapse();
                
                _isExpanded = value;

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
                if (ScrollOnCreate)
                    _host.ScrollIntoView(this);
            }
        }

        public bool ScrollOnCreate { get; set; }

        public event Action<ListTreeNodeView, IList<ListTreeNodeView>>? LoadChildren;

        public event Action<ListTreeNodeView>? SelectionChanged;

        public bool IsVisible => _parent == null || _parent.IsExpanded;

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
        protected internal BulkObservableCollection<ListTreeNodeView> _items;
        protected internal HashSet<ListTreeNodeView> _selectedItems;

        private int _updateCount;
        private int? _minUpdateIndex;

        public ListTreeView()
        {
            _items = [];
            _selectedItems = [];
        }

        public void BeginUpdate(ListTreeNodeView? refItem = null, bool isMassive =false)
        {
            if (isMassive)
                _items.BeginUpdate();   

            _updateCount++;

            if (refItem != null && refItem._index != -1)
                _minUpdateIndex = Math.Min(_minUpdateIndex ?? int.MaxValue, refItem._index);

        }

        public void EndUpdate(bool isMassive = false)
        {
            _updateCount--;

            if (_updateCount == 0)
            {
                RebuildIndexes(_minUpdateIndex ?? 0);
                _minUpdateIndex = null;
            }

            if (isMassive)
                _items.EndUpdate();

        }

        protected internal void RebuildIndexes(int startIndex = 0)
        {
            for (var i = startIndex; i < _items.Count; i++)
            {
                _items[i]._index = i;
                _items[i]._childInsertIndex = -1;
            }
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

        public IEditorUIElement? UIElement { get; set; }

        public IReadOnlySet<ListTreeNodeView> SelectedItems => _selectedItems;

        public ObservableCollection<ListTreeNodeView> Items => _items;
    }
}

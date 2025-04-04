﻿using System.ComponentModel;
using System.Diagnostics;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{

    [Panel("6cc81a05-0e2d-4782-b9c3-0d8ab46550ca")]
    [DisplayName("Outline")]
    public class OutlinePanel : BasePanel
    {
        static protected Dictionary<INode, ListTreeNodeView> _listNodeMap = [];

        protected SceneView? _sceneView;
        protected readonly ListTreeView _treeView;
        protected readonly NodeManager _nodeFactory;
        protected readonly SelectionManager _selection;

        public OutlinePanel()
        {
            Instance = this;
            _nodeFactory = Context.Require<NodeManager>();
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
            _treeView = new ListTreeView();
        }

        public void ExpandNode(INode target)
        {
            var nodeList = new List<INode>();
            var curNode = target;
            while (curNode != null)
            {
                nodeList.Insert(0, curNode);
                curNode = curNode.Parent;
            }

            var lastNode = nodeList[^1];

            foreach (var node in nodeList)
            {
                if (!_listNodeMap.TryGetValue(node, out var listNode))
                    continue;

                if (node != lastNode)
                {
                    listNode.IsExpanded = true;
                }
                else
                {
                    _treeView.ScrollIntoView(listNode);
                }
            }

        }

        private void OnSelectionChanged(ListTreeNodeView obj)
        {
            Debug.Assert(_mainDispatcher.IsCurrentThread);

            var node = ((NodeView)obj.Header!).Node;

            bool curSelected = _selection.IsSelected(node);

            if (!obj.IsSelected && curSelected)
                _selection.Items.Remove(node);

            else if (obj.IsSelected && !curSelected)
                _selection.Items.Add(node);
        }

        private void OnSelectionChanged(IReadOnlyCollection<INode> newSelection)
        {
            foreach (var curSel in _treeView.SelectedItems.ToArray())
            {
                var node = ((NodeView)curSel.Header!).Node;
                if (!newSelection.Contains(node))
                    curSel.IsSelected = false;
            }

            foreach (var item in newSelection)
            {
                if (_listNodeMap.TryGetValue(item, out var listNode))
                    listNode.IsSelected = true;
            }

            if (newSelection.Count == 1)
                ExpandNode(newSelection.First());
        }

        protected internal ListTreeNodeView? CreateNode(object? value, ListTreeNodeView? parent)
        {
            if (value == null)
                return null;

            var node = _nodeFactory.CreateNode(value);

            if (!_listNodeMap.TryGetValue(node, out var listNode))
            {
                listNode = new ListTreeNodeView(_treeView, parent);
                listNode.SelectionChanged += OnSelectionChanged;
                listNode.Header = new NodeView(listNode, this, node);
                listNode.IsSelected = _selection.IsSelected(node);

                _listNodeMap[node] = listNode;
            }
            else
            {
                listNode.Parent = parent;
                if (listNode.IsExpanded)
                    listNode.Unload();
            }


            return listNode;
        }


        protected override async Task LoadAsync()
        {
            _sceneView = await _panelManager.PanelAsync<SceneView>();
            _sceneView.SceneChanged += OnSceneChanged;

            LoadScene();

            await base.LoadAsync();
        }

        private void OnSceneChanged(Scene3D? obj)
        {
            LoadScene();
        }

        protected void LoadScene()
        {
            var root = CreateNode(_sceneView?.Scene, null);
            _treeView.Items.Clear();
            if (root != null)
                _treeView.Items.Add(root!);
        }

        public static OutlinePanel? Instance { get; internal set; }

        public ListTreeView TreeView => _treeView;

        public override string? Title => "Outline";
    }
}

﻿using SkiaSharp;
using System.Collections.ObjectModel;
using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public enum PropertiesEditorMode
    {
        Selection,
        Custom
    }

    public class PropertiesEditor : BasePanel
    {
        private INode? _activeNode;
        private readonly ObservableCollection<PropertiesGroupView> _groups = [];
        private readonly List<PropertyView> _props = [];
        private SKBitmap? _nodePreview;
        private IDispatcher? _renderDispatcher;
        private int _isUpdatingProps;

        public PropertiesEditor(PropertiesEditorMode mode, Guid panelId)
        {
            _panelId = panelId;

            Mode = mode;

            if (panelId == TOOLS)
                Title = "Tools";
            else if (panelId == PROPERTIES)
                Title = "Properties";

            if (mode == PropertiesEditorMode.Selection)
                Context.Require<SelectionManager>().Changed += OnSelectionChanged;

        }

        private async void OnSelectionChanged(IReadOnlyCollection<INode> items)
        {
            var obj = items.Select(a => a.Value).OfType<EngineObject>().FirstOrDefault();

            ActiveNode = obj != null ? Context.Require<NodeManager>().CreateNode(obj) : null;

            if (ActiveNode is IItemPreview preview)
                NodePreview = await preview.CreatePreviewAsync();
            else
                NodePreview = null;
        }


        public bool NodePreviewVisible => NodePreview != null;

        protected PropertiesGroupView? CreateProps(INode node)
        {
            if (node is not IEditorProperties editorProps)
                return null;

            if (node is INodeChanged changed)
                changed.NodeChanged += OnNodeChanged;

            var result = new PropertiesGroupView(PropertiesGroupType.Main)
            {
                Node = node
            };

            if (node is IItemView view)
            {
                if (node.Value is IComponent comp)
                    result.Header = new ComponentHeaderView(comp)
                    {
                        Name = view.DisplayName,
                        Icon = view.Icon,
                        OnRemove = () => _groups.Remove(result)
                    };
                else
                    result.Header = view.DisplayName;
            }

            _props.Clear();

            editorProps.EditorProperties(_props);
            if (editorProps.AutoGenerate)
                PropertyView.CreateProperties(node.Value, node.Value.GetType(), _props, node as INotifyPropertyChanged);

            var propsCats = _props.GroupBy(a => a.Category);

            foreach (var cat in propsCats)
            {
                if (string.IsNullOrEmpty(cat.Key))
                    result.Properties = cat.ToArray();
                else
                {
                    var catGrp = new PropertiesGroupView(PropertiesGroupType.Inner)
                    {
                        Header = cat.Key,
                        Properties = cat.ToArray(),
                        Node = node,
                    };
                    result.Groups ??= new List<PropertiesGroupView>();
                    result.Groups.Add(catGrp);

                }
            }

            foreach (var prop in _props)
                prop.Editor!.ValueChanged += OnValueChanged;

            var actions = new List<ActionView>();
            if (node is IEditorActions nodeActions)
                nodeActions.EditorActions(actions);

            ActionView.CreateActions(node.Value, actions);

            result.Actions = actions;

            return result;
        }

        private async void OnValueChanged(IPropertyEditor obj)
        {
            if (ActiveNode is IItemPreview preview)
                NodePreview = await preview.CreatePreviewAsync();
        }

        protected IEnumerable<PropertyView> EnumProps(INode? target = null)
        {
            var result = Enumerable.Empty<PropertyView>();

            void Visit(IEnumerable<PropertiesGroupView> groups)
            {
                foreach (var grp in groups)
                {
                    if (target != null && grp.Node != target && grp.Node?.Parent != target)
                        continue;
                    if (grp.Properties != null)
                        result = result.Concat(grp.Properties);
                    if (grp.Groups != null)
                        Visit(grp.Groups);
                }
            }

            if (_groups != null)
                Visit(_groups);

            return result;
        }

        protected void UpdateProperties()
        {
            _groups.Clear();

            if (_activeNode != null)
            {
                var mainGrp = CreateProps(_activeNode);
                if (mainGrp != null && mainGrp.Properties.Count > 0)
                {
                    mainGrp.Header = _activeNode.Types.First();
                    _groups.Add(mainGrp);
                }

                foreach (var compo in _activeNode.Components)
                {
                    var group = CreateProps(compo);
                    if (group != null)
                        _groups.Add(group);
                }
            }
        }

        protected void Detach()
        {
            void Visit(IEnumerable<PropertiesGroupView> groups)
            {
                foreach (var grp in groups)
                {
                    if (grp.Node is INodeChanged changed)
                        changed.NodeChanged -= OnNodeChanged;

                    if (grp.Groups != null)
                        Visit(grp.Groups);
                }
            }

            Visit(_groups);
        }

        protected void Attach()
        {
            if (_activeNode?.Value is EngineObject obj)
            {
                ToolBar = new ToolbarView();
                ToolBar.AddButton("icon_add", async () =>
                {
                    var picker = new ItemPickerView();
                    picker.ItemsSource = new ComponentsSource(obj);

                    var selItem = await picker.ShowAsync("Add component");

                    if (selItem != null)
                        await AddComponentAsync((TypeInfo)selItem);
                });

                OnPropertyChanged(nameof(ToolBar));
            }
        }

        protected async Task AddComponentAsync(TypeInfo type)
        {
            var comp = (IComponent)type.CreateInstance()!;

            var obj = (EngineObject)_activeNode!.Value;

            await EngineApp.Current!.Dispatcher.ExecuteAsync(() => obj.AddComponent(comp));

            var grp = CreateProps(comp.GetNode());

            if (grp != null)
                _groups.Add(grp);
        }

        protected virtual void OnNodeChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingProps > 0)
                return;

            var props = EnumProps((INode)sender!);

            var updates = new List<Action>();

            foreach (var prop in props)
            {
                if (prop.Editor?.Binding == null)
                    continue;
                var newValue = prop.Editor.Binding.Value;
                if (!Equals(newValue, prop.Editor.Value))
                    updates.Add(() => prop.Editor.Value = newValue!);
            }

            if (updates.Count == 0)
                return;

            _isUpdatingProps++;

            _renderDispatcher ??= EngineApp.Current!.Dispatcher;

            _renderDispatcher.ExecuteAsync(() =>
            {
                foreach (var update in updates)
                    update();
                _isUpdatingProps--;
            });
        }

        public ObservableCollection<PropertiesGroupView> Groups => _groups;

        public INode? ActiveNode
        {
            get => _activeNode;
            set
            {
                if (_activeNode == value)
                    return;
                if (_activeNode != null)
                    Detach();
                _activeNode = value;
                OnPropertyChanged(nameof(ActiveNode));
                OnPropertyChanged(nameof(NodeName));
                OnPropertyChanged(nameof(NodeIcon));
                UpdateProperties();
                Attach();
            }
        }

        public string NodeName
        {
            get
            {
                if (_activeNode is IItemView view)
                    return view.DisplayName;
                return string.Empty;
            }
            set
            {
                if (value == NodeName)
                    return;

                if (_activeNode is INameEdit edit)
                    edit.Name = value;
            }
        }

        public IconView? NodeIcon
        {
            get
            {
                if (_activeNode is IItemView view)
                    return view.Icon;
                return null;
            }
        }

        public SKBitmap? NodePreview
        {
            get => _nodePreview;
            set
            {
                if (_nodePreview == value)
                    return;
                _nodePreview = value;
                OnPropertyChanged(nameof(NodePreview));
                OnPropertyChanged(nameof(NodePreviewVisible));
            }
        }

        public PropertiesEditorMode Mode { get; }

        public override string? Title { get; }


        public static readonly Guid PROPERTIES = new("3fc8a4fb-806b-49cd-b770-ec127c8e5f79");

        public static readonly Guid TOOLS = new("d50e5a2d-dd41-4de2-9327-6b01e676cdc9");

    }
}

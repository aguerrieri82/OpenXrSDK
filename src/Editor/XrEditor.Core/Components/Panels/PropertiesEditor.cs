using SkiaSharp;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class PropertiesEditor : BasePanel
    {
        private INode? _activeNode;
        private IList<PropertiesGroupView>? _groups;
        private SKBitmap? _nodePreview;

        public PropertiesEditor()
        {
            Instance = this;
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

        public INode? ActiveNode
        {
            get => _activeNode;
            set
            {
                if (_activeNode == value)
                    return;
                _activeNode = value;
                OnPropertyChanged(nameof(ActiveNode));
                OnPropertyChanged(nameof(NodeName));
                OnPropertyChanged(nameof(NodeIcon));
                UpdateProperties();
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

        public bool NodePreviewVisible => NodePreview != null;

        protected PropertiesGroupView? CreateProps(INode node)
        {
            if (node is not IEditorProperties editorProps)
                return null;

            var result = new PropertiesGroupView(PropertiesGroupType.Main);

            if (node is IItemView view)
            {
                if (node.Value is IComponent comp)
                    result.Header = new ComponentHeaderView(comp)
                    {
                        Name = view.DisplayName,
                        Icon = view.Icon,
                    };
                else
                    result.Header = view.DisplayName;
            }

            var props = new List<PropertyView>();

            editorProps.EditorProperties(props);
            if (editorProps.AutoGenerate)
                PropertyView.CreateProperties(node.Value, node.Value.GetType(), props);

            var propsCats = props.GroupBy(a => a.Category);

            foreach (var cat in propsCats)
            {
                if (string.IsNullOrEmpty(cat.Key))
                    result.Properties = cat.ToArray();
                else
                {
                    var catGrp = new PropertiesGroupView(PropertiesGroupType.Inner)
                    {
                        Header = cat.Key,
                        Properties = cat.ToArray()
                    };
                    result.Groups ??= new List<PropertiesGroupView>();
                    result.Groups.Add(catGrp);
                }
            }

            foreach (var prop in result.Properties)
                prop.Editor!.ValueChanged += OnValueChanged;

            return result;
        }

        private async void OnValueChanged(IPropertyEditor obj)
        {
            if (ActiveNode is IItemPreview preview)
                NodePreview = await preview.CreatePreviewAsync();
        }

        protected void UpdateProperties()
        {
            var result = new List<PropertiesGroupView>();

            if (_activeNode != null)
            {
                var mainGrp = CreateProps(_activeNode);
                if (mainGrp != null && mainGrp.Properties.Count > 0)
                {
                    mainGrp.Header = _activeNode.Types.First();
                    result.Add(mainGrp);
                }


                foreach (var compo in _activeNode.Components)
                {
                    var group = CreateProps(compo);
                    if (group != null)
                        result.Add(group);
                }
            }

            Groups = result;
        }

        public IList<PropertiesGroupView>? Groups
        {
            get => _groups;
            set
            {
                _groups = value;
                OnPropertyChanged(nameof(Groups));
            }
        }

        public static PropertiesEditor? Instance { get; internal set; }
    }
}

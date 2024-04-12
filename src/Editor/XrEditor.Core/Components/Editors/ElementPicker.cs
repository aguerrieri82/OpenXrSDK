using CanvasUI;
using SkiaSharp;
using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor
{
    public class ElementPicker : BaseEditor<EngineObject?, EngineObject?>
    {
        protected INode? _node;
        protected string? _name;
        protected SKBitmap? _image;
        protected readonly RenderPreviewCreator _previewCreator;

        public ElementPicker()
        {
            _previewCreator = Context.Require<RenderPreviewCreator>();
        }

        public ElementPicker(IProperty<EngineObject?> binding)
            : this()
        {

            Binding = binding;
        }

        protected override async void OnEditValueChanged(EngineObject? newValue)
        {
            _node = newValue == null ? null : Context.Require<NodeManager>().CreateNode(newValue);

            if (_node is IItemView itemView)
                Name = itemView.DisplayName;
            else
                Name = string.Empty;

            if (_node is IItemPreview preview)
                Image = await preview.CreatePreviewAsync();
            else
                Image = null;

            base.OnEditValueChanged(newValue);
        }


        public SKBitmap? Image
        {
            get => _image;
            set
            {
                if (_image == value)
                    return;
                _image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        public string? Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }


        public static ElementPicker Create<T>(IProperty<T?> binding) where T : EngineObject
        {
            return new ElementPicker(binding!.Convert(new CastConverter<T, EngineObject?>()));
        }
    }

    public struct EngineObjectEditorFactory : IPropertyEditorFactory
    {
        public bool CanHandle(Type type)
        {
            return typeof(EngineObject).IsAssignableFrom(type);
        }

        public IPropertyEditor CreateEditor(Type type, IEnumerable<Attribute> attributes)
        {
            return new ElementPicker();
        }
    }
}

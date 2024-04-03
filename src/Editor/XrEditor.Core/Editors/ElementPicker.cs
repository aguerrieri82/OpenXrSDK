using CanvasUI;
using SkiaSharp;
using System.Numerics;
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

        public ElementPicker(IProperty<EngineObject?> binding)
        {
            _previewCreator = Context.Require<RenderPreviewCreator>();
            Binding = binding;
        }

        protected override async void OnEditValueChanged(EngineObject? newValue)
        {
            _node = newValue == null ? null : Context.Require<NodeFactory>().CreateNode(newValue);

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
}

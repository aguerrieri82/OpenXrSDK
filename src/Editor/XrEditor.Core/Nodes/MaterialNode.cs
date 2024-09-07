using SkiaSharp;
using UI.Binding;
using XrEditor.Services;
using XrEngine;


namespace XrEditor.Nodes
{
    public class MaterialNode<T> : EngineObjectNode<T>, IItemPreview where T : Material
    {
        public MaterialNode(T value) : base(value)
        {
            _autoGenProps = true;
        }


        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            base.EditorProperties(binder, curProps);
            PropertyView.CreateProperties(_value, typeof(Material), curProps);
        }

        public async Task<SKBitmap?> CreatePreviewAsync()
        {
            var preview = Context.Require<RenderPreviewCreator>();

            return await preview.Engine.Dispatcher.ExecuteAsync(() => preview.CreateMaterial(_value));
        }

        public override IconView? Icon => new()
        {
            Color = "#689F38",
            Name = "icon_image"
        };

        public override string DisplayName => _value.Name ?? _value.GetType().Name;

    }
}

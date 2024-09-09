using SkiaSharp;
using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Texture2DNode : EngineObjectNode<Texture2D>, IItemPreview
    {
        public Texture2DNode(Texture2D value)
            : base(value)
        {

        }

        protected override void EditorProperties(Binder<Texture2D> binder, IList<PropertyView> curProps)
        {

            base.EditorProperties(binder, curProps);
            PropertyView.CreateProperties(_value, typeof(Texture), curProps);
            PropertyView.CreateProperties(_value, typeof(Texture2D), curProps);
        }

        public async Task<SKBitmap?> CreatePreviewAsync()
        {
            try
            {
                var preview = Context.Require<RenderPreviewCreator>();

                return await preview.Engine.Dispatcher.ExecuteAsync(() => preview.CreateTexture(_value));
            }
            catch
            {
                return null;
            }
        }

        public override string DisplayName => _value.Name ?? base.DisplayName;

    }
}

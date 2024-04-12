using SkiaSharp;
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

        public override string DisplayName => _value.Name ?? base.DisplayName;

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
    }
}

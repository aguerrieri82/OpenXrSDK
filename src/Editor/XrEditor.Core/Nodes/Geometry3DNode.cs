using SkiaSharp;
using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Geometry3DNode : EngineObjectNode<Geometry3D>, IItemPreview
    {
        public Geometry3DNode(Geometry3D value)
            : base(value)
        {

        }

        public async Task<SKBitmap> CreatePreviewAsync()
        {
            var preview = Context.Require<RenderPreviewCreator>();

            return await preview.Engine.Dispatcher.ExecuteAsync(() => preview.CreateGeometry(_value));
        }
    }
}

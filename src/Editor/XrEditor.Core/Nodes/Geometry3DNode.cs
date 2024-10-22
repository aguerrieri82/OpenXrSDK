using SkiaSharp;
using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Geometry3DNode : EngineObjectNode<Geometry3D>, IItemPreview, INotifyPropertyChanged
    {
        public Geometry3DNode(Geometry3D value)
            : base(value)
        {
            _autoGenProps = true;
        }



        public async Task<SKBitmap?> CreatePreviewAsync()
        {
            var preview = Context.Require<RenderPreviewCreator>();

            return await preview.Engine.Dispatcher.ExecuteAsync(() => preview.CreateGeometry(_value));
        }

        public void NotifyPropertyChanged(IProperty property)
        {
            if (_value is IGeneratedContent generated)
                generated.Build();
        }


        public override IconView? Icon => new()
        {
            Color = "#aaaaaa",
            Name = "icon_category",
            Filled = false
        };
    }
}

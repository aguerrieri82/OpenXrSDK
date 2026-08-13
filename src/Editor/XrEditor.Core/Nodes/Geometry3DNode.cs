using UI.Binding;
using XrEditor.Services;
using XrEngine;

namespace XrEditor.Nodes
{
    public class Geometry3DNode<TVert> : EngineObjectNode<Geometry3D<TVert>>, IItemPreview, INotifyPropertyChanged
        where TVert : unmanaged, IVertexProvider
    {
        public Geometry3DNode(Geometry3D<TVert> value)
            : base(value)
        {
            _autoGenProps = PropertiesGenerationMode.All;
        }

        public async Task<NativeImage?> CreatePreviewAsync()
        {
            if (_value is not SimpleGeometry3D simp)
                return null;

            var preview = Context.Require<RenderPreviewCreator>();
            return await preview.Engine.Dispatcher.ExecuteAsync(() => preview.CreateGeometry(simp));
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

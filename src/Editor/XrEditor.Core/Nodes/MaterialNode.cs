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
        }


        protected override void EditorProperties(Binder<T> binder, IList<PropertyView> curProps)
        {
            binder.PropertyChanged += (_, _, _, _) => _value.NotifyChanged(ObjectChangeType.Render);

            curProps.Add(new PropertyView
            {
                Label = "Use Depth",
                Editor = new BoolEditor(binder.Prop(a => a.UseDepth))
            });


            curProps.Add(new PropertyView
            {
                Label = "Write Depth",
                Editor = new BoolEditor(binder.Prop(a => a.WriteDepth))
            });

            curProps.Add(new PropertyView
            {
                Label = "Double Sided",
                Editor = new BoolEditor(binder.Prop(a => a.DoubleSided))
            });

            curProps.Add(new PropertyView
            {
                Label = "Write Color",
                Editor = new BoolEditor(binder.Prop(a => a.WriteColor))
            });

            curProps.Add(new PropertyView
            {
                Label = "Alpha",
                Editor = new EnumEditor<AlphaMode>(binder.Prop(a => a.Alpha))
            });

            base.EditorProperties(binder, curProps);
        }

        public async Task<SKBitmap> CreatePreviewAsync()
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

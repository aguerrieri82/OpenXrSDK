using UI.Binding;
using XrEditor.Abstraction;
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

        public async Task<NativeImage?> CreatePreviewAsync()
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

        public override void Actions(IList<ActionView> result)
        {
            base.Actions(result);
            result.Add(new ActionView
            {
                DisplayName = "Save",
                IsEnabled = true,
                ExecuteCommand = new Command(async () =>
                {
                    var preview = await CreatePreviewAsync();
                    if (preview != null)
                    {
                        //using var file = File.OpenWrite("d:\\out.png");
                        //preview.Encode(SKEncodedImageFormat.Png, 100).SaveTo(file);
                    }
                })
            });
        }

        public override string DisplayName => _value.Name ?? base.DisplayName;

    }
}

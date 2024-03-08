namespace OpenXr.Framework
{
    public static class XrExtensions
    {
        public static void AddProjection(this XrLayerManager manager, RenderViewDelegate renderView)
        {
            manager.Layers.Add(new XrProjectionLayer(renderView));
        }
    }
}

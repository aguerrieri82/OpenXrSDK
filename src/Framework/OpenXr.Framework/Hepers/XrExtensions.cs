using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;

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

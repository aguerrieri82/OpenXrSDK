using Android.Content;
using OpenXr.Framework.Android;
using Silk.NET.OpenXR;
using XrInteraction;

namespace OpenXr.Framework
{
    public static class XrExtensions
    {
        public static XrQuadLayer AddQuad(this XrLayerManager layers, Extent2Di size, GetQuadDelegate getQuad)
        {
            var source = new XrAndroidSurfaceLayerSource(size);
            return layers.Add(new XrQuadLayer(getQuad, source));
        }

        public static XrWebViewLayerSource AddWebView(this XrLayerManager layers, Context context, GetQuadDelegate getQuad, ISurfaceInput surfaceInput)
        {
            var quad = getQuad();

            var size = new Extent2Di
            {
                Width = XrWebViewLayerSource.AlignToMultiple((int)(quad.Size.X * 1700), 32),
                Height = XrWebViewLayerSource.AlignToMultiple((int)(quad.Size.Y * 1700), 32)
            };

            var source = new XrWebViewLayerSource(context, size, surfaceInput);

            layers.Add(new XrQuadLayer(getQuad, source)
            {
                FlipY = true
            });

            return source;
        }
    }
}

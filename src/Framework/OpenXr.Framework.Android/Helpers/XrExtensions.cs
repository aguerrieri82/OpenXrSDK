using Android.Content;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using Silk.NET.OpenXR;
using XrInteraction;


namespace OpenXr.Framework
{
    public static unsafe class XrExtensions
    {
        public static XrAndroidSurfaceQuadLayer AddQuad(this XrLayerManager layers, Extent2Di size, GetQuadDelegate getQuad)
        {
            return layers.Add(new XrAndroidSurfaceQuadLayer(size, getQuad));
        }

        public static XrWebViewLayer AddWebView(this XrLayerManager layers, Context context, GetQuadDelegate getQuad, ISurfaceInput surfaceInput)
        {
            return layers.Add(new XrWebViewLayer(context, getQuad, surfaceInput));
        }
    }
}

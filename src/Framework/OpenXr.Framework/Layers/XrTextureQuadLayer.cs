using Common.Interop;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework
{
    public unsafe delegate bool RenderQuadDelegate(SwapchainImageBaseHeader* image, Size2I size, long predTime);

    public class XrTextureQuadLayer : XrBaseQuadLayer
    {
        protected RenderQuadDelegate _renderQuad;
        protected Size2I _size;
        protected NativeArray<SwapchainImageBaseHeader>? _images;

        public XrTextureQuadLayer(GetQuadDelegate getQuad, RenderQuadDelegate renderQuad, Size2I size)
            : base(getQuad)
        {
            _renderQuad = renderQuad;
            _size = size;

        }

        public unsafe override void Create()
        {
            Debug.Assert(_xrApp != null);

            var extent = new Extent2Di((int)_size.Width, (int)_size.Height);

            _swapchain = _xrApp!.CreateSwapChain(extent, _xrApp.RenderOptions.ColorFormat, 1, SwapchainUsageFlags.SampledBit | SwapchainUsageFlags.ColorAttachmentBit);

            _images = _xrApp.EnumerateSwapchainImages(_swapchain);

            _header.ValueRef.SubImage.Swapchain = _swapchain;
            _header.ValueRef.SubImage.ImageArrayIndex = 0;
            _header.ValueRef.SubImage.ImageRect.Extent = extent;
            _header.ValueRef.EyeVisibility = EyeVisibility.Both;
            _header.ValueRef.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;

            base.Create();
        }


        protected unsafe override bool Update(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            Debug.Assert(_xrApp != null);
            Debug.Assert(_images != null);

            if (!base.Update(ref layer, ref views, predTime))
                return false;

            if (!_renderQuad(null, new Size2I(), 0))
                return true;

            var index = _xrApp.AcquireSwapchainImage(_swapchain);

            _xrApp.WaitSwapchainImage(_swapchain);

            try
            {
                return _renderQuad(_images.ItemPointer((int)index), _size, predTime);
            }
            finally
            {
                _xrApp.ReleaseSwapchainImage(_swapchain);
            }
        }


        public Size2I Size => _size;

    }
}

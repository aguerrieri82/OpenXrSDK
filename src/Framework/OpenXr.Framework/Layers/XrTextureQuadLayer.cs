using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace OpenXr.Framework.Layers
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

            _swapchain = _xrApp!.CreateSwapChain(extent, 1, _xrApp.RenderOptions.SwapChainFormat, 1);

            _images = _xrApp.EnumerateSwapchainImages(_swapchain);

            _header->SubImage.Swapchain = _swapchain;
            _header->SubImage.ImageArrayIndex = 0;
            _header->SubImage.ImageRect.Extent = extent;
            _header->EyeVisibility = EyeVisibility.Both;
            _header->LayerFlags = CompositionLayerFlags.None;

            base.Create();
        }

        protected unsafe override bool Update(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            if (!_renderQuad(null, new Size2I(), 0))
                return true;

            Debug.Assert(_xrApp != null);
            Debug.Assert(_images != null);

            base.Update(ref layer, ref views, predTime);

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

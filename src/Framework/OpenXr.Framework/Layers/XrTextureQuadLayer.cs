using Common.Interop;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Xml.Linq;
using XrMath;

namespace OpenXr.Framework
{
    public unsafe delegate bool RenderQuadDelegate(SwapchainImageBaseHeader* image, Size2I size, long predTime, int eye);

    public class XrTextureQuadLayer : XrBaseQuadLayer
    {
        protected RenderQuadDelegate _renderQuad;
        protected Size2I _size;
        protected NativeArray<SwapchainImageBaseHeader>? _images;
        protected int _eye;
        protected XrSwapchain? _swapchain;

        public XrTextureQuadLayer(GetQuadDelegate getQuad, RenderQuadDelegate renderQuad, Size2I size)
            : base(getQuad)
        {
            _renderQuad = renderQuad;
            _size = size;
            _eye = -1;
        }

        public void ConfigureStereo(XrSwapchain swapchain, int eye)
        {
            _swapchain = swapchain;
            _eye = eye;
        }

        public override void Create()
        {
            Debug.Assert(_xrApp != null);

            var extent = new Extent2Di((int)_size.Width, (int)_size.Height);

            _swapchain ??= new XrSwapchain(_xrApp, 1);

            if (!_swapchain.IsCreated)
            {
                _swapchain.Create(extent,
                    _xrApp!.RenderOptions.ColorFormat, _eye != -1 ? 2u : 1u,
                    SwapchainUsageFlags.SampledBit | SwapchainUsageFlags.ColorAttachmentBit, 1, false);
            }

            _images = _swapchain.EnumerateImages();

            _header.ValueRef.SubImage.Swapchain = _swapchain;
            _header.ValueRef.SubImage.ImageArrayIndex = _eye == -1 ? 0 : (uint)_eye;
            _header.ValueRef.SubImage.ImageRect.Extent = extent;
            _header.ValueRef.EyeVisibility = _eye == -1 ? EyeVisibility.Both : (_eye == 0 ? EyeVisibility.Left : EyeVisibility.Right);
            _header.ValueRef.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;

            base.Create();
        }


        protected unsafe override bool Update(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            Debug.Assert(_xrApp != null);
            Debug.Assert(_images != null);
            Debug.Assert(_swapchain != null);

            if (!base.Update(ref layer, ref views, predTime))
                return false;

            if (!_renderQuad(null, new Size2I(), 0, _eye))
                return true;

            var index = _swapchain.Acquire();

            _swapchain.Wait();

            try
            {
                return _renderQuad(_images.ItemPointer((int)index), _size, predTime, _eye);
            }
            finally
            {
                _swapchain.Release();
            }
        }

        public override void Destroy()
        {
            _swapchain?.Dispose();
            _swapchain = null;
            base.Destroy();
        }

        public Size2I Size => _size;

    }
}

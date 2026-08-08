using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework
{
    public unsafe delegate bool RenderQuadDelegate(QuadRenderData data, SwapchainImageBaseHeader* image, long predTime);

    public class QuadRenderData
    {
        public XrSwapchain? Swapchain;

        public int Eye;
    }

    public class XrTextureQuadLayer : XrBaseQuadLayer
    {
        protected RenderQuadDelegate _renderQuad;
        protected Size2I _size;
        protected XrSwapchain? _swapchain;
        protected QuadRenderData _data;


        public XrTextureQuadLayer(GetQuadDelegate getQuad, RenderQuadDelegate renderQuad, Size2I size)
            : base(getQuad)
        {
            _renderQuad = renderQuad;
            _size = size;

            _data = new QuadRenderData
            {
                Eye = -1
            };
        }

        public void ConfigureStereo(XrSwapchain swapchain, int eye)
        {
            _swapchain = swapchain;
            _data.Eye = eye;
            _data.Swapchain = swapchain;
        }

        public override void Create()
        {
            Debug.Assert(_xrApp != null);

            var extent = new Extent2Di((int)_size.Width, (int)_size.Height);

            if (Format == 0)
                Format = _xrApp.RenderOptions.ColorFormat;

            _swapchain ??= new XrSwapchain(_xrApp, 1);

            _data.Swapchain = _swapchain;

            if (!_swapchain.IsCreated)
            {
                _swapchain.Create(extent,
                    Format,
                    _data.Eye != -1 ? 2u : 1u,
                    SwapchainUsageFlags.SampledBit |
                    SwapchainUsageFlags.ColorAttachmentBit |
                    SwapchainUsageFlags.InputAttachmentBitKhr |
                    SwapchainUsageFlags.TransferSrcBit |
                    SwapchainUsageFlags.TransferDstBit,
                    SwapchainTarget.Quad);
            }

            _header.ValueRef.SubImage.Swapchain = _swapchain;
            _header.ValueRef.SubImage.ImageArrayIndex = _data.Eye == -1 ? 0 : (uint)_data.Eye;
            _header.ValueRef.SubImage.ImageRect.Extent = extent;
            _header.ValueRef.EyeVisibility = _data.Eye == -1 ? EyeVisibility.Both : (_data.Eye == 0 ? EyeVisibility.Left : EyeVisibility.Right);
            _header.ValueRef.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }


        protected unsafe override bool Update(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            Debug.Assert(_xrApp != null && _swapchain != null);

            if (!base.Update(ref layer, ref views, predTime))
                return false;

#warning TODO: COPY THE OLD FRAME INSTEAD!

            /*
            if (!_renderQuad(null, new Size2I(), 0, _eye))
                return false;
            */

            var image = _swapchain.AcquireImageAndWait();

            try
            {
                return _renderQuad(_data, image, predTime);
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

        public int Format { get; set; }
    }
}

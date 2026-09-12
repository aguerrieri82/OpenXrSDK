using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework
{
    public unsafe delegate bool RenderGeometryLayerDelegate(GeometryRenderData data, SwapchainImageBaseHeader* image, long predTime);

    public class GeometryRenderData
    {
        public XrSwapchain? Swapchain;

        public int Eye;
    }

    public class XrTextureLayerSource : IGeometryLayerSource
    {
        protected RenderGeometryLayerDelegate _render;
        protected Size2I _size;
        protected XrSwapchain? _swapchain;
        protected GeometryRenderData _data;
        protected XrApp? _xrApp;

        public XrTextureLayerSource(RenderGeometryLayerDelegate render, Size2I size)
        {
            _render = render;
            _size = size;

            _data = new GeometryRenderData
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

        public void Initialize(XrApp app, IList<string> extensions)
        {
            _xrApp = app;
        }

        public SwapchainSubImage Create()
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

            return new SwapchainSubImage
            {
                Swapchain = _swapchain,
                ImageArrayIndex = _data.Eye == -1 ? 0 : (uint)_data.Eye,
                ImageRect =
                {
                    Extent = extent
                }
            };
        }

        public unsafe bool Update(long predTime)
        {
            Debug.Assert(_swapchain != null);

            var image = _swapchain.AcquireImageAndWait();

            try
            {
                return _render(_data, image, predTime);
            }
            finally
            {
                _swapchain.Release();
            }
        }

        public void OnBeginFrame(Space space, long displayTime)
        {
        }

        public void OnEndFrame()
        {
        }

        public void Destroy()
        {
            _swapchain?.Dispose();
            _swapchain = null;
        }

        public Size2I Size => _size;

        public int Format { get; set; }
    }
}
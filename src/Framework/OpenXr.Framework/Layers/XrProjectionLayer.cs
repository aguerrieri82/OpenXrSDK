using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{

    public unsafe delegate void RenderViewDelegate(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader*[] images, XrRenderMode mode, long predTime);



    public unsafe class XrProjectionLayer : XrBaseLayer<CompositionLayerProjection>
    {
        readonly RenderViewDelegate? _renderView;
        protected XrSwapchainInfo[]? _swapchains;

        XrProjectionLayer()
        {
            _header->Type = StructureType.CompositionLayerProjection;
            _header->LayerFlags =
                CompositionLayerFlags.CorrectChromaticAberrationBit |
                CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }

        public XrProjectionLayer(RenderViewDelegate renderView)
            : this()
        {
            _renderView = renderView;
        }

        public override void Destroy()
        {
            if (_header->Views != null)
            {
                Marshal.FreeHGlobal(new nint(_header->Views));
                _header->Views = null;
            }
        }

        public override void Create()
        {
            Debug.Assert(_xrApp != null);

            int swpCount = _xrApp.RenderOptions.RenderMode == XrRenderMode.SingleEye ? _xrApp.ViewInfo!.ViewCount : 1;

            _swapchains = new XrSwapchainInfo[swpCount];

            for (var i = 0; i < _swapchains.Length; i++)
            {
                var swapchain = _xrApp.CreateSwapChain();
                _swapchains[i] = new XrSwapchainInfo
                {
                    Swapchain = swapchain,
                    Images = _xrApp.EnumerateSwapchainImages(swapchain),
                    ViewSize = _xrApp.RenderOptions.Size
                };
            }


            base.Create();
        }

        protected override bool Update(ref CompositionLayerProjection layer, ref View[] views, long predTime)
        {
            Debug.Assert(_xrApp != null);
            Debug.Assert(_swapchains != null);

            if (layer.Views == null)
            {
                layer.Views = (CompositionLayerProjectionView*)Marshal.AllocHGlobal(sizeof(CompositionLayerProjectionView) * views.Length);
                layer.ViewCount = (uint)views.Length;

                for (var i = 0; i < views.Length; i++)
                {
                    ref CompositionLayerProjectionView projView = ref layer.Views[i];

                    int swIndex = 0;

                    if (_xrApp.RenderOptions.RenderMode == XrRenderMode.SingleEye)
                        swIndex = i;

                    var swapchain = _swapchains[swIndex];
                    int swOfs = 0;

                    if (_xrApp.RenderOptions.RenderMode == XrRenderMode.Stereo)
                        swOfs = i * swapchain.ViewSize.Width;

                    projView.Type = StructureType.CompositionLayerProjectionView;
                    projView.Next = null;
                    projView.SubImage.Swapchain = swapchain.Swapchain;
                    projView.SubImage.ImageRect.Offset.X = swOfs;
                    projView.SubImage.ImageRect.Offset.Y = 0;
                    projView.SubImage.ImageRect.Extent.Height = swapchain.ViewSize.Height;
                    projView.SubImage.ImageRect.Extent.Width = swapchain.ViewSize.Width;

                    if (_xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                        projView.SubImage.ImageArrayIndex = (uint)i;
                    else
                        projView.SubImage.ImageArrayIndex = 0;
                }
            }

            var c1 = layer.Views[1];


            var projViews = new Span<CompositionLayerProjectionView>(layer.Views, (int)layer.ViewCount);

            if (_renderView != null)
                return Render(ref projViews, ref views, _swapchains, predTime);

            return false;
        }

        protected bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            var images = new SwapchainImageBaseHeader*[swapchains.Length];

            for (var i = 0; i < images.Length; i++)
            {
                var swc = swapchains[i];
                var index = _xrApp!.AcquireSwapchainImage(swapchains[i].Swapchain);
                images[i] = swc.Images!.ItemPointer((int)index);
                _xrApp.WaitSwapchainImage(swapchains[i].Swapchain);
            }

            for (var i = 0; i < views.Length; i++)
            {
                ref CompositionLayerProjectionView projView = ref projViews[i];
                projView.Fov = views[i].Fov;
                projView.Pose = views[i].Pose;
            }

            try
            {
                _renderView!(ref projViews, images, _xrApp!.RenderOptions.RenderMode, predTime);
            }
            catch (Exception ex)
            {
                _xrApp!.Logger.LogError(ex, "Render failed: {ex}", ex);
                return false;
            }
            finally
            {
                foreach (var sw in swapchains)
                    _xrApp!.ReleaseSwapchainImage(sw.Swapchain);
            }

            return true;
        }


        public override void Dispose()
        {
            if (_swapchains != null)
            {
                foreach (var item in _swapchains)
                {
                    _xrApp?.CheckResult(_xrApp.Xr.DestroySwapchain(item.Swapchain), "DestroySwapchain");
                    item.Images?.Dispose();
                }

                _swapchains = null;
            }

            base.Dispose();
        }

        public IEnumerable<Swapchain> SwapChains => _swapchains?.Select(a => a.Swapchain) ?? [];

    }
}

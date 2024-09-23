using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{

    public unsafe delegate void RenderViewDelegate(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader*[] colorImages, SwapchainImageBaseHeader*[]? depthImages, XrRenderMode mode, long predTime);

    public unsafe class XrProjectionLayer : XrBaseLayer<CompositionLayerProjection>
    {
        protected readonly RenderViewDelegate? _renderView;
        protected XrSwapchainInfo[]? _swapchains;
        protected bool _useDepthSWC = false;

        XrProjectionLayer()
        {
            _header->Type = StructureType.CompositionLayerProjection;
            _header->LayerFlags =
                CompositionLayerFlags.CorrectChromaticAberrationBit |
                CompositionLayerFlags.BlendTextureSourceAlphaBit;
            Priority = 10;
        }

        public XrProjectionLayer(RenderViewDelegate renderView)
            : this()
        {
            _renderView = renderView;
        }

        public override void Destroy()
        {
            if (_header != null && _header->Views != null)
            {
                var viewCount = _xrApp?.ViewInfo!.ViewCount;

                for (var i = 0; i < _header->ViewCount; i++)
                {
                    if (_header->Views[i].Next != null)
                        Marshal.FreeHGlobal(new nint(_header->Views[i].Next));
                }

                Marshal.FreeHGlobal(new nint(_header->Views));

                _header->Views = null;
            }

            if (_swapchains != null)
            {
                foreach (var item in _swapchains)
                {
                    _xrApp?.CheckResult(_xrApp.Xr.DestroySwapchain(item.ColorSwapchain), "DestroySwapchain");

                    if (item.DepthSwapchain.Handle != 0)
                        _xrApp?.CheckResult(_xrApp.Xr.DestroySwapchain(item.DepthSwapchain), "DestroySwapchain");

                    item.ColorImages?.Dispose();
                    item.DepthImages?.Dispose();
                }

                _swapchains = null;
            }

            _header->Space.Handle = 0;

        }

        public override void Create()
        {
            Debug.Assert(_xrApp != null);

            int swpCount = _xrApp.RenderOptions.RenderMode == XrRenderMode.SingleEye ? _xrApp.ViewInfo!.ViewCount : 1;

            _swapchains = new XrSwapchainInfo[swpCount];

            for (var i = 0; i < _swapchains.Length; i++)
            {
                var colorSwap = _xrApp.CreateSwapChain(false);
                var depthSwap = _useDepthSWC ? _xrApp.CreateSwapChain(true) : new Swapchain();
                _swapchains[i] = new XrSwapchainInfo
                {
                    ColorSwapchain = colorSwap,
                    DepthSwapchain = depthSwap,
                    ColorImages = _xrApp.EnumerateSwapchainImages(colorSwap),
                    DepthImages = _useDepthSWC ? _xrApp.EnumerateSwapchainImages(depthSwap) : null,
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
                    projView.SubImage.Swapchain = swapchain.ColorSwapchain;
                    projView.SubImage.ImageRect.Offset.X = swOfs;
                    projView.SubImage.ImageRect.Offset.Y = 0;
                    projView.SubImage.ImageRect.Extent.Height = swapchain.ViewSize.Height;
                    projView.SubImage.ImageRect.Extent.Width = swapchain.ViewSize.Width;

                    if (_useDepthSWC)
                    {
                        var depthInfo = (CompositionLayerDepthInfoKHR*)Marshal.AllocHGlobal(sizeof(CompositionLayerDepthInfoKHR));
                        depthInfo->Type = StructureType.CompositionLayerDepthInfoKhr;
                        depthInfo->MinDepth = 0;
                        depthInfo->MaxDepth = 1;
                        depthInfo->NearZ = 0;
                        depthInfo->FarZ = 0;
                        depthInfo->Next = null;
                        depthInfo->SubImage.Swapchain = swapchain.DepthSwapchain;
                        depthInfo->SubImage.ImageRect = projView.SubImage.ImageRect;

                        projView.Next = depthInfo;

                        if (_xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                        {
                            depthInfo->SubImage.ImageArrayIndex = (uint)i;
                        }
                        else
                        {
                            depthInfo->SubImage.ImageArrayIndex = 0;
                        }
                    }

                    if (_xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                    {
                        projView.SubImage.ImageArrayIndex = (uint)i;
                    }
                    else
                    {
                        projView.SubImage.ImageArrayIndex = 0;
                    }
                }
            }

            var projViews = new Span<CompositionLayerProjectionView>(layer.Views, (int)layer.ViewCount);

            if (_renderView != null)
                return Render(ref projViews, ref views, _swapchains, predTime);

            return false;
        }

        protected bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            var colorImages = new SwapchainImageBaseHeader*[swapchains.Length];
            var depthImages = _useDepthSWC ? new SwapchainImageBaseHeader*[swapchains.Length] : null;

            for (var i = 0; i < colorImages.Length; i++)
            {
                var swc = swapchains[i];

                var colorIndex = _xrApp!.AcquireSwapchainImage(swapchains[i].ColorSwapchain);
                colorImages[i] = swc.ColorImages!.ItemPointer((int)colorIndex);
                _xrApp.WaitSwapchainImage(swapchains[i].ColorSwapchain);

                if (_useDepthSWC)
                {
                    var depthIndex = _xrApp!.AcquireSwapchainImage(swapchains[i].DepthSwapchain);
                    depthImages![i] = swc.DepthImages!.ItemPointer((int)depthIndex);
                    _xrApp.WaitSwapchainImage(swapchains[i].DepthSwapchain);
                }
            }

            for (var i = 0; i < views.Length; i++)
            {
                ref CompositionLayerProjectionView projView = ref projViews[i];
                projView.Fov = views[i].Fov;
                projView.Pose = views[i].Pose;
            }

            try
            {
                _renderView!(ref projViews, colorImages, depthImages, _xrApp!.RenderOptions.RenderMode, predTime);
            }
            catch (Exception ex)
            {
                _xrApp!.Logger.LogError(ex, "Render failed: {ex}", ex);
                return false;
            }
            finally
            {
                foreach (var sw in swapchains)
                {
                    _xrApp!.ReleaseSwapchainImage(sw.ColorSwapchain);
                    if (_useDepthSWC)
                        _xrApp!.ReleaseSwapchainImage(sw.DepthSwapchain);
                }
            }

            return true;
        }


        public IEnumerable<Swapchain> ColorSwapChains => _swapchains?.Select(a => a.ColorSwapchain) ?? [];

        public bool UseDepthSwapchain
        {
            get => _useDepthSWC;
            set => _useDepthSWC = value;
        }

    }
}

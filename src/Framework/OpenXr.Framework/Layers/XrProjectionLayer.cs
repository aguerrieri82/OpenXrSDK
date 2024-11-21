using Common.Interop;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System.Diagnostics;

namespace OpenXr.Framework
{
    public unsafe ref struct RenderViewInfo
    {
        public Span<CompositionLayerProjectionView> ProjViews;

        public SwapchainImageBaseHeader*[] ColorImages;

        public SwapchainImageBaseHeader*[]? DepthImages;

        public XrRenderMode Mode;

        public long DisplayTime;

    }

    public unsafe delegate void RenderViewDelegate(ref RenderViewInfo info);

    public unsafe class XrProjectionLayer : XrBaseLayer<CompositionLayerProjection>
    {
        protected readonly RenderViewDelegate? _renderView;
        protected XrSwapchainInfo[]? _swapchains;
        protected bool _useDepthSWC = false;
        protected NativeArray<CompositionLayerDepthInfoKHR> _depthInfo;
        protected NativeArray<CompositionLayerProjectionView> _projViews;

        XrProjectionLayer()
        {
            _depthInfo = new NativeArray<CompositionLayerDepthInfoKHR>(2, typeof(CompositionLayerDepthInfoKHR));
            _projViews = new NativeArray<CompositionLayerProjectionView>(2, typeof(CompositionLayerProjectionView));

            _header.ValueRef.Type = StructureType.CompositionLayerProjection;
            _header.ValueRef.LayerFlags =
                CompositionLayerFlags.CorrectChromaticAberrationBit |
                CompositionLayerFlags.BlendTextureSourceAlphaBit;
            Priority = 10;
        }

        public XrProjectionLayer(RenderViewDelegate renderView)
            : this()
        {
            _renderView = renderView;
        }

        public override void Dispose()
        {
            _depthInfo.Dispose();
            _projViews.Dispose();
            base.Dispose();
        }

        public override void Destroy()
        {

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

            _header.ValueRef.Space.Handle = 0;
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

        protected override bool Update(ref CompositionLayerProjection layer, ref View[] views, long displayTime)
        {
            Debug.Assert(_xrApp != null);
            Debug.Assert(_swapchains != null);

            if (layer.Views == null)
            {
                layer.Views = _projViews.ItemPointer(0);
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
                        var depthInfo = _depthInfo.ItemPointer(i);
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

                    UpdateView(ref projView, i);
                }
            }

            var projViews = new Span<CompositionLayerProjectionView>(layer.Views, (int)layer.ViewCount);

            if (_renderView != null)
                return Render(ref projViews, ref views, _swapchains, displayTime);

            return false;
        }

        protected virtual void UpdateView(ref CompositionLayerProjectionView projView, int index)
        {
        }

        protected virtual bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
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
                var info = new RenderViewInfo
                {
                    ProjViews = projViews,
                    ColorImages = colorImages,
                    DepthImages = depthImages,
                    Mode = _xrApp!.RenderOptions.RenderMode,
                    DisplayTime = predTime
                };

                _renderView!(ref info);
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

using Common.Interop;
using Microsoft.Extensions.Logging;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;

namespace OpenXr.Framework
{
    public ref struct RenderViewsInfo
    {
        public XrProjectionLayer Layer;

        public Span<CompositionLayerProjectionView> ProjViews;

        public unsafe SwapchainImageBaseHeader*[] ColorImages;

        public unsafe SwapchainImageBaseHeader*[]? DepthImages;

        public XrSwapchain[] Color;

        public XrSwapchain[]? Depth;

        public XrRenderMode Mode;

        public Extent2Di? RenderedSize;

        public Vector2 CropScale;

        public Fovf SharedFov;

        public long DisplayTime;
    }

    public delegate void RenderViewDelegate(ref RenderViewsInfo info);

    public unsafe class XrProjectionLayer : XrBaseLayer<CompositionLayerProjection>
    {
        protected readonly RenderViewDelegate? _renderView;

        protected bool _useDepth;
        protected NativeArray<CompositionLayerDepthInfoKHR> _depthInfo;
        protected NativeArray<CompositionLayerProjectionView> _projViews;
        protected NativeStruct<CompositionLayerDepthTestFB> _depthTest;
        protected SwapchainImageBaseHeader*[]? _lastColorImages;
        protected SwapchainImageBaseHeader*[]? _lastDepthImages;
        protected XrSwapchain[]? _colorSwaps;
        protected XrSwapchain[]? _depthSwaps;
        protected Fovf _sharedFov;
        protected Vector2 _sharedFovScale;

        XrProjectionLayer()
        {
            _depthInfo = new NativeArray<CompositionLayerDepthInfoKHR>(2, typeof(CompositionLayerDepthInfoKHR));
            _projViews = new NativeArray<CompositionLayerProjectionView>(2, typeof(CompositionLayerProjectionView));

            _header.ValueRef.Type = StructureType.CompositionLayerProjection;

            _header.ValueRef.LayerFlags =
                CompositionLayerFlags.CorrectChromaticAberrationBit |
                CompositionLayerFlags.BlendTextureSourceAlphaBit;

            Priority = XrLayerPriority.Projection;

            if (XrDevice.IsMetaQuest)
                UseSimmetricFov = true;
        }

        public XrProjectionLayer(RenderViewDelegate renderView, bool useDepth)
            : this()
        {
            _renderView = renderView;
            _useDepth = useDepth;

            if (_useDepth)
            {
                _depthTest.Value = new CompositionLayerDepthTestFB
                {
                    Type = StructureType.CompositionLayerDepthTestFB,
                    DepthMask = 1,
                    CompareOp = CompareOpFB.LessOrEqualFB,
                    Next = null
                };

                StructChain.AddNextStruct(ref _header.ValueRef, _depthTest.Pointer);
            }
        }

        public override void Destroy()
        {
            if (_colorSwaps != null)
            {
                foreach (var item in _colorSwaps)
                    item.Dispose();
            }

            if (_depthSwaps != null)
            {
                foreach (var item in _depthSwaps)
                    item.Dispose();
            }

            _header.ValueRef.Space.Handle = 0;
        }

        public override void Create()
        {
            Debug.Assert(_xrApp != null);

            if (UseSimmetricFov)
            {
                var views = new View[2];
                views[0].Type = StructureType.View;
                views[1].Type = StructureType.View;

                var now = _xrApp.XrNow();

                _xrApp.LocateViews(_xrApp.ReferenceSpace, now, views);

                var fovs = views.Select(a => a.Fov).ToArray();

                _sharedFov = BuildSharedFov(fovs);

                _sharedFovScale = GetSharedFovScale(fovs);
            }

            var options = _xrApp.RenderOptions;

            var swpCount = options.RenderMode == XrRenderMode.SingleEye ? _xrApp.ViewInfo!.ViewCount : 1;

            _colorSwaps = new XrSwapchain[swpCount];

            if (_useDepth)
                _depthSwaps = new XrSwapchain[swpCount];

            var colorSize = AdjustRenderSize(options.Size);

            var depthSize = new Extent2Di((int)(colorSize.Width * options.ProjectionDepthScale),
                                          (int)(colorSize.Height * options.ProjectionDepthScale));

            for (var i = 0; i < _colorSwaps.Length; i++)
            {
                var colorSwap = new XrSwapchain(_xrApp);

                colorSwap.Create(colorSize,
                            options.ColorFormat,
                            options.RenderMode == XrRenderMode.MultiView ? 2u : 1,
                            SwapchainUsageFlags.ColorAttachmentBit |
                            SwapchainUsageFlags.SampledBit |
                            SwapchainUsageFlags.InputAttachmentBitKhr, SwapchainTarget.Projection);

                _colorSwaps[i] = colorSwap;

                if (_useDepth)
                {
                    var depthSwap = new XrSwapchain(_xrApp);

                    depthSwap.Create(depthSize,
                           options.DepthFormat,
                           options.RenderMode == XrRenderMode.MultiView ? 2u : 1,
                           SwapchainUsageFlags.DepthStencilAttachmentBit |
                           SwapchainUsageFlags.SampledBit |
                           SwapchainUsageFlags.InputAttachmentBitKhr, SwapchainTarget.Projection);

                    _depthSwaps![i] = depthSwap;
                }
            }

            base.Create();
        }

        protected static Fovf BuildSharedFov(Fovf[] fovs)
        {
            var maxX = 0f;
            var maxUp = 0f;
            var maxDown = 0f;

            for (var i = 0; i < fovs.Length; i++)
            {
                var fov = fovs[i];
                maxX = MathF.Max(maxX, MathF.Max(MathF.Abs(MathF.Tan(fov.AngleLeft)), MathF.Abs(MathF.Tan(fov.AngleRight))));
                maxUp = MathF.Max(maxUp, MathF.Abs(MathF.Tan(fov.AngleUp)));
                maxDown = MathF.Max(maxDown, MathF.Abs(MathF.Tan(fov.AngleDown)));
            }

            return new Fovf
            {
                AngleLeft = -MathF.Atan(maxX),
                AngleRight = MathF.Atan(maxX),
                AngleUp = MathF.Atan(maxUp),
                AngleDown = -MathF.Atan(maxDown)
            };
        }

        protected static Vector2 GetSharedFovScale(Fovf[] fovs)
        {
            var maxX = 0f;
            for (var i = 0; i < fovs.Length; i++)
                maxX = MathF.Max(maxX, MathF.Max(MathF.Abs(MathF.Tan(fovs[i].AngleLeft)), MathF.Abs(MathF.Tan(fovs[i].AngleRight))));

            var fov = fovs[0];
            var width = MathF.Abs(MathF.Tan(fov.AngleLeft)) + MathF.Abs(MathF.Tan(fov.AngleRight));

            return new Vector2(2f * maxX / width, 1f);
        }

        public Extent2Di AdjustRenderSize(Extent2Di size)
        {
            if (!UseSimmetricFov)
                return size;

            return new Extent2Di
            {
                Width = (int)MathF.Ceiling(size.Width * _sharedFovScale.X),
                Height = (int)MathF.Ceiling(size.Height * _sharedFovScale.Y)
            };
        }

        protected override bool Update(ref CompositionLayerProjection layer, ref View[] views, long displayTime)
        {
            Debug.Assert(_xrApp != null);
            Debug.Assert(_colorSwaps != null);

            if (layer.Views == null)
            {
                layer.Views = _projViews.ItemPointer(0);
                layer.ViewCount = (uint)views.Length;

                for (var i = 0; i < views.Length; i++)
                {
                    ref var projView = ref layer.Views[i];

                    var swIndex = 0;

                    if (_xrApp.RenderOptions.RenderMode == XrRenderMode.SingleEye)
                        swIndex = i;

                    var colorSwap = _colorSwaps[swIndex];

                    projView.Type = StructureType.CompositionLayerProjectionView;
                    projView.Next = null;
                    projView.SubImage.Swapchain = colorSwap;

                    if (_xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                        projView.SubImage.ImageArrayIndex = (uint)i;
                    else
                        projView.SubImage.ImageArrayIndex = 0;

                    if (_useDepth)
                    {
                        var depthSwap = _depthSwaps![swIndex];

                        var depthInfo = _depthInfo.ItemPointer(i);

                        depthInfo->Type = StructureType.CompositionLayerDepthInfoKhr;
                        depthInfo->Next = null;
                        depthInfo->MinDepth = 0;
                        depthInfo->MaxDepth = 1;
                        depthInfo->SubImage.Swapchain = depthSwap;

                        StructChain.AddNextStruct(ref projView, depthInfo);

                        if (_xrApp.RenderOptions.RenderMode == XrRenderMode.MultiView)
                            depthInfo->SubImage.ImageArrayIndex = (uint)i;
                        else
                            depthInfo->SubImage.ImageArrayIndex = 0;
                    }

                    UpdateView(ref projView, i);
                }
            }

            var projViews = new Span<CompositionLayerProjectionView>(layer.Views, (int)layer.ViewCount);

            if (_renderView != null)
                return Render(ref projViews, ref views, displayTime);

            return false;
        }

        protected virtual void UpdateView(ref CompositionLayerProjectionView projView, int index)
        {
        }

        protected virtual void Acquire(ref Span<CompositionLayerProjectionView> projViews)
        {
            Debug.Assert(_colorSwaps != null);

            _lastColorImages = new SwapchainImageBaseHeader*[_colorSwaps.Length];
            _lastDepthImages = _useDepth ? new SwapchainImageBaseHeader*[_colorSwaps.Length] : null;

            for (var i = 0; i < _lastColorImages.Length; i++)
            {
                _lastColorImages[i] = _colorSwaps[i].AcquireImageAndWait();

                if (_useDepth)
                    _lastDepthImages![i] = _depthSwaps![i].AcquireImageAndWait();
            }
        }

        protected virtual void Release()
        {
            Debug.Assert(_colorSwaps != null);

            foreach (var item in _colorSwaps)
                item.Release();

            if (_depthSwaps != null)
            {
                foreach (var item in _depthSwaps)
                    item.Release();
            }

            _lastColorImages = null;
            _lastDepthImages = null;
        }

        protected virtual bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, long predTime)
        {
            Debug.Assert(_colorSwaps != null);

            Acquire(ref projViews);

            for (var i = 0; i < views.Length; i++)
            {
                ref var projView = ref projViews[i];
                projView.Fov = views[i].Fov;
                projView.Pose = views[i].Pose;
            }

            try
            {
                var info = new RenderViewsInfo
                {
                    ProjViews = projViews,
                    ColorImages = _lastColorImages!,
                    DepthImages = _lastDepthImages,
                    Mode = _xrApp!.RenderOptions.RenderMode,
                    Color = _colorSwaps,
                    Depth = _depthSwaps,
                    DisplayTime = predTime,
                    Layer = this,
                    CropScale = _sharedFovScale,
                    SharedFov = _sharedFov
                };

                _renderView!(ref info);

                var renderSize = _colorSwaps[0].Size;

                if (info.RenderedSize != null)
                    renderSize = info.RenderedSize.Value;

                for (var i = 0; i < _projViews.Length; i++)
                {
                    ref var view = ref _projViews[i];

                    var colorOffset = 0;

                    if (_xrApp!.RenderOptions.RenderMode == XrRenderMode.Stereo)
                        colorOffset = i * _colorSwaps[0].Size.Width;

                    if (UseSimmetricFov)
                    {
                        var cropW = (int)MathF.Round(renderSize.Width / info.CropScale.X);
                        var x = i == 0 ? 0 : renderSize.Width - cropW;

                        view.SubImage.ImageRect.Offset.X = colorOffset + x;
                        view.SubImage.ImageRect.Offset.Y = 0;
                        view.SubImage.ImageRect.Extent.Width = cropW;
                        view.SubImage.ImageRect.Extent.Height = renderSize.Height;
                    }
                    else
                    {
                        view.SubImage.ImageRect.Offset.X = colorOffset;
                        view.SubImage.ImageRect.Offset.Y = 0;
                        view.SubImage.ImageRect.Extent = renderSize;
                    }

                    if (_useDepth)
                    {
                        var depth = _depthInfo.ItemPointer(i);
                        var depthSize = _depthSwaps![0].Size;
                        var depthOffset = 0;

                        if (_xrApp.RenderOptions.RenderMode == XrRenderMode.Stereo)
                            depthOffset = i * depthSize.Width;

                        if (UseSimmetricFov)
                        {
                            var cropW = (int)MathF.Round(depthSize.Width / info.CropScale.X);
                            var x = i == 0 ? 0 : depthSize.Width - cropW;

                            depth->SubImage.ImageRect.Offset.X = depthOffset + x;
                            depth->SubImage.ImageRect.Offset.Y = 0;
                            depth->SubImage.ImageRect.Extent.Width = cropW;
                            depth->SubImage.ImageRect.Extent.Height = depthSize.Height;
                        }
                        else
                        {
                            depth->SubImage.ImageRect.Offset.X = depthOffset;
                            depth->SubImage.ImageRect.Offset.Y = 0;
                            depth->SubImage.ImageRect.Extent = depthSize;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _xrApp!.Logger.LogError(ex, "Render failed: {ex}", ex);

                return false;
            }
            finally
            {
                Release();
            }

            return true;
        }

        public override void Dispose()
        {
            _depthInfo.Dispose();
            _projViews.Dispose();
            _depthTest.Dispose();

            if (_colorSwaps != null)
            {
                foreach (var item in _colorSwaps)
                    item.Dispose();
            }

            if (_depthSwaps != null)
            {
                foreach (var item in _depthSwaps)
                    item.Dispose();
            }

            base.Dispose();
        }

        public XrSwapchain[] ColorSwapchains => _colorSwaps ?? throw new InvalidOperationException();

        public XrSwapchain[]? DepthSwapchains => _depthSwaps;

        public bool UseSimmetricFov { get; set; }

        public bool UseDepth
        {
            get => _useDepth;
            set => _useDepth = value;
        }
    }
}
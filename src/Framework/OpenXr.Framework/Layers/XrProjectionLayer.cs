using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using Silk.NET.Windowing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{

    public unsafe delegate void RenderViewDelegate(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image, int viewIndex, long predTime);

    public unsafe delegate void RenderMultiViewDelegate(ref Span<CompositionLayerProjectionView> projViews, SwapchainImageBaseHeader* image, long predTime);


    public unsafe class XrProjectionLayer : BaseXrLayer<CompositionLayerProjection>
    {
        readonly RenderViewDelegate? _renderView;
        readonly RenderMultiViewDelegate? _renderMultiView;

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

        public XrProjectionLayer(RenderMultiViewDelegate renderMultiView)
            : this()
        {
            _renderMultiView = renderMultiView;
        }

        public override void Dispose()
        {
            if (_header->Views != null)
            {
                Marshal.FreeHGlobal(new nint(_header->Views));
                _header->Views = null;
            }

            base.Dispose();
        }

        protected override bool Render(ref CompositionLayerProjection layer, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            Debug.Assert(_xrApp != null);

            if (layer.Views == null)
            {
                layer.Views = (CompositionLayerProjectionView*)Marshal.AllocHGlobal(sizeof(CompositionLayerProjectionView) * views.Length);
                layer.ViewCount = (uint)views.Length;
            }

            var projViews = new Span<CompositionLayerProjectionView>(layer.Views, (int)layer.ViewCount);

            if (_renderView != null)
                return RenderSingleView(ref projViews, ref views, swapchains, predTime);

            if (_renderMultiView != null)
                return RenderMultiView(ref projViews, ref views, swapchains, predTime);

            return false;
        }

        protected bool RenderMultiView(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            ref var swapChainInfo = ref swapchains[0];

            var index = _xrApp!.AcquireSwapchainImage(swapChainInfo.Swapchain);

            _xrApp.WaitSwapchainImage(swapChainInfo.Swapchain);
            try
            {
                for (var i = 0; i < views.Length; i++)
                {
                    ref var view = ref views[i];
                    ref CompositionLayerProjectionView projView = ref projViews[i];

                    projView.Type = StructureType.CompositionLayerProjectionView;
                    projView.Next = null;
                    projView.Fov = view.Fov;
                    projView.Pose = view.Pose;
                    projView.SubImage.Swapchain = swapChainInfo.Swapchain;
                    projView.SubImage.ImageArrayIndex = (uint)i;
                    projView.SubImage.ImageRect.Offset.X = 0;
                    projView.SubImage.ImageRect.Offset.Y = 0;
                    projView.SubImage.ImageRect.Extent = swapChainInfo.Size;

                    Debug.Assert(swapChainInfo.Images != null);
                }

                _renderMultiView!(ref projViews, swapChainInfo.Images!.ItemPointer((int)index), predTime);

            }
            catch (Exception ex)
            {
                _xrApp.Logger.LogError(ex, "Render failed: {ex}", ex);
                return false;
            }
            finally
            {
                _xrApp.ReleaseSwapchainImage(swapChainInfo.Swapchain);
            }

            return true;
        }

        protected bool RenderSingleView(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {

            for (var i = 0; i < views.Length; i++)
            {
                ref var swapChainInfo = ref swapchains[i];
                ref var view = ref views[i];
                ref CompositionLayerProjectionView projView = ref projViews[i];

                var index = _xrApp!.AcquireSwapchainImage(swapChainInfo.Swapchain);
                try
                {
                    _xrApp.WaitSwapchainImage(swapChainInfo.Swapchain);

                    projView.Type = StructureType.CompositionLayerProjectionView;
                    projView.Next = null;
                    projView.Fov = view.Fov;
                    projView.Pose = view.Pose;
                    projView.SubImage.Swapchain = swapChainInfo.Swapchain;
                    projView.SubImage.ImageArrayIndex = 0;
                    projView.SubImage.ImageRect.Offset.X = 0;
                    projView.SubImage.ImageRect.Offset.Y = 0;
                    projView.SubImage.ImageRect.Extent = swapChainInfo.Size;

                    Debug.Assert(swapChainInfo.Images != null);

                    _renderView!(ref projView, swapChainInfo.Images.ItemPointer((int)index), i, predTime);
                }
                catch (Exception ex)
                {
                    _xrApp.Logger.LogError(ex, "Render failed: {ex}", ex);
                    return false;
                }
                finally
                {
                    _xrApp.ReleaseSwapchainImage(swapChainInfo.Swapchain);
                }
            }

            return true;
        }

    }
}

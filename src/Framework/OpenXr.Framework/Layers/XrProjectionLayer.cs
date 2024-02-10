using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenXr.Framework
{

    public unsafe delegate void RenderViewDelegate(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image, long predTime);

    public unsafe class XrProjectionLayer : BaseXrLayer<CompositionLayerProjection>
    {
        readonly RenderViewDelegate _renderView;

        public XrProjectionLayer(RenderViewDelegate renderView)
        {
            _renderView = renderView;
            _header->Type = StructureType.CompositionLayerProjection;
            _header->LayerFlags =
                CompositionLayerFlags.CorrectChromaticAberrationBit |
                CompositionLayerFlags.BlendTextureSourceAlphaBit;
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

            for (var i = 0; i < views.Length; i++)
            {
                ref var swapChainInfo = ref swapchains[i];
                ref var view = ref views[i];
                ref CompositionLayerProjectionView projView = ref projViews[i];

                var index = _xrApp.AcquireSwapchainImage(swapChainInfo.Swapchain);
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

                    _renderView(ref projView, swapChainInfo.Images.ItemPointer((int)index), predTime);
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

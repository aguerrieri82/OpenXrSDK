using OpenXr.Framework;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{

    public unsafe delegate void RenderViewDelegate(ref CompositionLayerProjectionView view, SwapchainImageBaseHeader* image, long predTime);

    public unsafe class XrProjectionLayer : BaseXrLayer<CompositionLayerProjection>
    {
        RenderViewDelegate _renderView;

        public XrProjectionLayer(XrApp xrApp, RenderViewDelegate renderView)
            : base(xrApp)
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

                    _renderView(ref projView, swapChainInfo.Images!.ItemPointer((int)index), predTime);
                }
                catch (Exception ex)
                {
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

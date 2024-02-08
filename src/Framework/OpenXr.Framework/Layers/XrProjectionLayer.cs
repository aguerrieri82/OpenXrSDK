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

    public delegate void RenderViewDelegate(ref CompositionLayerProjectionView view, NativeArray<SwapchainImageBaseHeader> images);

    public unsafe class XrProjectionLayer : BaseXrLayer<CompositionLayerProjection>
    {
        RenderViewDelegate _renderView;

        public XrProjectionLayer(XrApp xrApp, RenderViewDelegate renderView)
            : base(xrApp)
        {
            _renderView = renderView;
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

        protected override bool Render(ref CompositionLayerProjection layer, ref View[] views, XrSwapchainInfo[] swapchains)
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

                    projView.Fov = view.Fov;
                    projView.Pose = view.Pose;
                    projView.SubImage.Swapchain = swapChainInfo.Swapchain;
                    projView.SubImage.ImageArrayIndex = index;
                    projView.SubImage.ImageRect.Offset.X = 0;
                    projView.SubImage.ImageRect.Offset.Y = 0;
                    projView.SubImage.ImageRect.Extent = swapChainInfo.Size;

                    _renderView(ref projView, swapChainInfo.Images!);
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

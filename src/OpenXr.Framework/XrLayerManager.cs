using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public unsafe class XrLayerManager
    {
        public unsafe class LayerInfo
        {
            public CompositionLayerBaseHeader* Header { get; set; }

            public bool IsEnabled { get; set; } 

        }

        List<LayerInfo>? _layers = new();
        XrApp _xrApp;

        public XrLayerManager(XrApp xrApp)
        {
            _xrApp = xrApp; 
        }

        public void AddProjection()
        {

        }

        protected void RenderProjection(ref CompositionLayerProjection layerProject, View[] views, XrSwapchainInfo[] swapchains)
        {
            for (var i = 0; i < views.Length; i++)
            {
                ref var swapChainInfo = ref swapchains[i];
                ref var view = ref views[i];
                ref CompositionLayerProjectionView projView = layerProject.Views[i];

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
                }
                finally
                {
                    _xrApp.ReleaseSwapchainImage(swapChainInfo.Swapchain);
                }
            }
        }

        public CompositionLayerBaseHeader*[] Render(View[] views, XrSwapchainInfo[] swapchains)
        {

        }
    }
}

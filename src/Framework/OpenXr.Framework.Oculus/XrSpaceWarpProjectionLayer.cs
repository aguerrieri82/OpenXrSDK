using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public class XrSpaceWarpProjectionLayer : XrProjectionLayer
    {
        private NativeArray<CompositionLayerSpaceWarpInfoFB> _spaceWarpInfo; 
        private Swapchain _motionColorSwapchain;
        private Swapchain _motionDepthSwapchain;
        private NativeArray<SwapchainImageBaseHeader>? _motionColorImages;
        private NativeArray<SwapchainImageBaseHeader>? _motionDepthImages;
        private IMotionVectorProvider _motionVector;
        private Extent2Di _motionImageSize;
        private readonly Pose3[] _lastPose = new Pose3[2];   

        public XrSpaceWarpProjectionLayer(RenderViewDelegate renderView, IMotionVectorProvider provider)
            : base(renderView)
        {
            _spaceWarpInfo = new NativeArray<CompositionLayerSpaceWarpInfoFB>(2, typeof(CompositionLayerSpaceWarpInfoFB));
            _motionVector = provider;   
        }

        public unsafe override void Create()
        {
            base.Create();

            Debug.Assert(_xrApp != null);

            var spaceWarpProperties = new SystemSpaceWarpPropertiesFB()
            {
                Type = StructureType.SystemSpaceWarpPropertiesFB
            };

            _xrApp.GetSystemProperties(ref spaceWarpProperties);

            _motionImageSize = new Extent2Di
            {
                Width = (int)spaceWarpProperties.RecommendedMotionVectorImageRectWidth,
                Height = (int)spaceWarpProperties.RecommendedMotionVectorImageRectHeight
            };

            if (_motionImageSize.Width == 0 || _motionImageSize.Height == 0)
                _motionImageSize = _xrApp.RenderOptions.Size;

            _motionColorSwapchain = _xrApp.CreateSwapChain(_motionImageSize, _motionVector.MotionVectorFormat, 2, SwapchainUsageFlags.ColorAttachmentBit | SwapchainUsageFlags.SampledBit);
            _motionColorImages = _xrApp.EnumerateSwapchainImages(_motionColorSwapchain);

            _motionDepthSwapchain = _xrApp.CreateSwapChain(_motionImageSize, _motionVector.DepthFormat, 2, SwapchainUsageFlags.DepthStencilAttachmentBit | SwapchainUsageFlags.SampledBit);
            _motionDepthImages = _xrApp.EnumerateSwapchainImages(_motionDepthSwapchain);
        }

        public override void Destroy()
        {
            base.Destroy();

            _xrApp?.DestroySwapchain(_motionColorSwapchain);
            _xrApp?.DestroySwapchain(_motionDepthSwapchain);

            _motionDepthImages?.Dispose();
            _motionColorImages?.Dispose();  
        }       

        protected unsafe override bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long displayTime)
        {
            Debug.Assert(_xrApp != null);

            var colorIndex = _xrApp.AcquireSwapchainImage(_motionColorSwapchain);
            var depthIndex = _xrApp.AcquireSwapchainImage(_motionDepthSwapchain);

            _xrApp.WaitSwapchainImage(_motionColorSwapchain);
            _xrApp.WaitSwapchainImage(_motionDepthSwapchain);

            try
            {
                var colorImage = _motionColorImages!.ItemPointer((int)colorIndex);
                var depthImage = _motionDepthImages!.ItemPointer((int)depthIndex);

                _motionVector.UpdateMotionVectors(ref projViews, colorImage, depthImage, _xrApp!.RenderOptions.RenderMode);

                /*
                for (var i = 0; i < projViews.Length; i++)
                {
                    var info = _spaceWarpInfo.ItemPointer(i);
                    var curPose = projViews[i].Pose.ToPose3();
                    var lastPose = _lastPose[i];
                    var delta = lastPose.Inverse().Multiply(curPose);
                    info->AppSpaceDeltaPose = delta.ToPoseF();  
                    _lastPose[i] = curPose; 
                }
                */

                if (!base.Render(ref projViews, ref views, swapchains, displayTime))
                    return false;

            }
            catch (Exception ex)
            {
                _xrApp!.Logger.LogError(ex, "Render failed: {ex}", ex);
                return false;
            }
            finally
            {
                _xrApp.ReleaseSwapchainImage(_motionColorSwapchain);
                _xrApp.ReleaseSwapchainImage(_motionDepthSwapchain);
            }

            return true; 
        }

        protected unsafe override void UpdateView(ref CompositionLayerProjectionView projView, int index)
        {
            var info = _spaceWarpInfo.ItemPointer(index);

            info->Type = StructureType.CompositionLayerSpaceWarpInfoFB;
            info->Next = null;
            
            info->DepthSubImage.Swapchain = _motionDepthSwapchain;
            info->DepthSubImage.ImageArrayIndex = (uint)index;
            info->DepthSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di { X = 0, Y = 0 },
                Extent = _motionImageSize
            };

            info->MotionVectorSubImage.Swapchain = _motionColorSwapchain;
            info->MotionVectorSubImage.ImageArrayIndex = (uint)index;
            info->MotionVectorSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di { X = 0, Y = 0 },
                Extent = _motionImageSize
            };

            info->MaxDepth = 1;
            info->MinDepth = 0;
            info->NearZ = _motionVector.Near;
            info->FarZ = _motionVector.Far;
            info->LayerFlags = CompositionLayerSpaceWarpInfoFlagsFB.None;

            info->AppSpaceDeltaPose = new Posef
            {
                Orientation = new Quaternionf { X = 0, Y = 0, Z = 0, W = 1 },
                Position = new Vector3f { X = 0, Y = 0, Z = 0 }
            };

            //projView.Next = info;
        }

        public override void Dispose()
        {
            _spaceWarpInfo.Dispose();
            base.Dispose();
        }
    }
}

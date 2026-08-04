using Common.Interop;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public class XrSpaceWarpProjectionLayer : XrProjectionLayer
    {

        readonly NativeArray<CompositionLayerSpaceWarpInfoFB> _spaceWarpInfo;
        readonly IXrMotionVectorProvider _motionProvider;
        readonly Pose3[] _lastPose = new Pose3[2];

        unsafe SwapchainImageBaseHeader* _spColorImage;
        unsafe SwapchainImageBaseHeader* _spDepthImage;
        SpaceWarpData _spaceWarpData;
        Extent2Di _motionImageSize;
        bool _lastSpaceWarpActive;

        public XrSpaceWarpProjectionLayer(RenderViewDelegate renderView, IXrMotionVectorProvider provider, bool useDepthSwapchain)
            : base(renderView, useDepthSwapchain)
        {
            _spaceWarpInfo = new NativeArray<CompositionLayerSpaceWarpInfoFB>(2, typeof(CompositionLayerSpaceWarpInfoFB));
            _motionProvider = provider;
            _spaceWarpData = new SpaceWarpData();
        }

        public override void Create()
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

            _spaceWarpData.ColorSwapchain = _xrApp.CreateSwapChain(
                          _motionImageSize,
                          _motionProvider.MotionVectorFormat, // Rgba16f
                          2,
                          SwapchainUsageFlags.ColorAttachmentBit | SwapchainUsageFlags.SampledBit);

            _spaceWarpData.ColorImages = _xrApp.EnumerateSwapchainImages(_spaceWarpData.ColorSwapchain);

            _spaceWarpData.DepthSwapchain = _xrApp.CreateSwapChain(
                _motionImageSize,
                _motionProvider.DepthFormat, // DepthComponent16
                2,
                SwapchainUsageFlags.DepthStencilAttachmentBit | SwapchainUsageFlags.SampledBit);

            _spaceWarpData.DepthImages = _xrApp.EnumerateSwapchainImages(_spaceWarpData.DepthSwapchain);
        }

        public override void Destroy()
        {
            base.Destroy();

            _xrApp?.DestroySwapchain(_spaceWarpData.DepthSwapchain);
            _xrApp?.DestroySwapchain(_spaceWarpData.ColorSwapchain);
        }

        protected unsafe override bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, long displayTime)
        {
            var isActive = _motionProvider.IsActive;

            if (isActive != _lastSpaceWarpActive)
            {
                for (var i = 0; i < _spaceWarpInfo.Length; i++)
                {
                    if (isActive)
                        StructChain.AddNextStruct(ref projViews[i], _spaceWarpInfo.ItemPointer(i));
                    else
                        StructChain.RemoveNextStruct(ref projViews[i], _spaceWarpInfo.ItemPointer(i));
                }
                _lastSpaceWarpActive = isActive;
            }

            if (!isActive)
                return base.Render(ref projViews, ref views, displayTime);

            Debug.Assert(_xrApp != null);

            var colorIndex = _xrApp.AcquireSwapchainImage(_spaceWarpData.ColorSwapchain);
            _xrApp.WaitSwapchainImage(_spaceWarpData.ColorSwapchain);

            var depthIndex = _xrApp.AcquireSwapchainImage(_spaceWarpData.DepthSwapchain);
            _xrApp.WaitSwapchainImage(_spaceWarpData.DepthSwapchain);

            _spColorImage = _spaceWarpData.ColorImages.ItemPointer((int)colorIndex);
            _spDepthImage = _spaceWarpData.DepthImages.ItemPointer((int)depthIndex);

            try
            {
                _motionProvider.UpdateMotionVectors(_spaceWarpData, _spColorImage, _spDepthImage, _xrApp.RenderOptions.RenderMode);

                for (var i = 0; i < projViews.Length; i++)
                {
                    var info = _spaceWarpInfo.ItemPointer(i);
                    info->LayerFlags = CompositionLayerSpaceWarpInfoFlagsFB.None;

                    var curPose = _xrApp.ReferenceFrame.Multiply(projViews[i].Pose.ToPose3());
                    var lastPose = _lastPose[i];

                    info->AppSpaceDeltaPose = lastPose.Inverse().Multiply(curPose).ToPoseF();

                    _lastPose[i] = curPose;
                }

                if (!base.Render(ref projViews, ref views, displayTime))
                    return false;

            }
            catch (Exception ex)
            {
                _xrApp!.Logger.LogError(ex, "Render failed: {ex}", ex);
                return false;
            }
            finally
            {
                _xrApp.ReleaseSwapchainImage(_spaceWarpData.ColorSwapchain);
                _xrApp.ReleaseSwapchainImage(_spaceWarpData.DepthSwapchain);
            }

            return true;
        }

        protected unsafe override void UpdateView(ref CompositionLayerProjectionView projView, int index)
        {
            var info = _spaceWarpInfo.ItemPointer(index);

            info->Type = StructureType.CompositionLayerSpaceWarpInfoFB;
            info->Next = null;

            info->DepthSubImage.Swapchain = _spaceWarpData.DepthSwapchain;
            info->DepthSubImage.ImageArrayIndex = (uint)index;
            info->DepthSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di { X = 0, Y = 0 },
                Extent = _motionImageSize
            };

            info->MotionVectorSubImage.Swapchain = _spaceWarpData.ColorSwapchain;
            info->MotionVectorSubImage.ImageArrayIndex = (uint)index;
            info->MotionVectorSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di { X = 0, Y = 0 },
                Extent = _motionImageSize
            };

            info->MaxDepth = 1;
            info->MinDepth = 0;
            info->NearZ = _motionProvider.Near; // 0.01
            info->FarZ = _motionProvider.Far; //10
            info->LayerFlags = CompositionLayerSpaceWarpInfoFlagsFB.None;

            info->AppSpaceDeltaPose = new Posef
            {
                Orientation = new Quaternionf { X = 0, Y = 0, Z = 0, W = 1 },
                Position = new Vector3f { X = 0, Y = 0, Z = 0 }
            };
        }

        public override void Dispose()
        {
            _spaceWarpData.DepthImages?.Dispose();
            _spaceWarpData.ColorImages?.Dispose();

            _spaceWarpInfo.Dispose();

            base.Dispose();
        }
    }
}

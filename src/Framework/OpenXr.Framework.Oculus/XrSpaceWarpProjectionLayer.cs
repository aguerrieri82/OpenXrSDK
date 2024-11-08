using Microsoft.Extensions.Logging;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public class XrSpaceWarpProjectionLayer : XrProjectionLayer
    {
        public struct SpaceWarpData
        {
            public Swapchain ColorSwapchain;
            public Swapchain DepthSwapchain;
            public NativeArray<SwapchainImageBaseHeader> ColorImages;
            public NativeArray<SwapchainImageBaseHeader> DepthImages;
        }

        private readonly NativeArray<CompositionLayerSpaceWarpInfoFB> _spaceWarpInfo;
        private readonly SpaceWarpData[] _spaceWarpData;
        private readonly unsafe SwapchainImageBaseHeader*[] _spColorImages;
        private readonly unsafe SwapchainImageBaseHeader*[] _spDepthImages;
        private readonly Pose3[] _lastPose = new Pose3[2];
        private readonly bool _warpTexArray = true;

        private readonly IMotionVectorProvider _motionProvider;
        private Extent2Di _motionImageSize;
        private bool _lastSpaceWarpActive;

        public unsafe XrSpaceWarpProjectionLayer(RenderViewDelegate renderView, IMotionVectorProvider provider)
            : base(renderView)
        {
            _spaceWarpInfo = new NativeArray<CompositionLayerSpaceWarpInfoFB>(2, typeof(CompositionLayerSpaceWarpInfoFB));
            _motionProvider = provider;
            _spaceWarpData = new SpaceWarpData[_warpTexArray ? 1 : 2];

            _spColorImages = new SwapchainImageBaseHeader*[_spaceWarpData.Length];
            _spDepthImages = new SwapchainImageBaseHeader*[_spaceWarpData.Length];
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

            for (var i = 0; i < _spaceWarpData.Length; i++)
            {
                _spaceWarpData[i].ColorSwapchain = _xrApp.CreateSwapChain(
                    _motionImageSize,
                    _motionProvider.MotionVectorFormat, // Rgba16f
                    _warpTexArray ? 2 : 1u,
                    SwapchainUsageFlags.ColorAttachmentBit | SwapchainUsageFlags.SampledBit, false);

                _spaceWarpData[i].ColorImages = _xrApp.EnumerateSwapchainImages(_spaceWarpData[i].ColorSwapchain);

                _spaceWarpData[i].DepthSwapchain = _xrApp.CreateSwapChain(
                    _motionImageSize,
                    _motionProvider.DepthFormat, // DepthComponent16
                    _warpTexArray ? 2 : 1u,
                    SwapchainUsageFlags.DepthStencilAttachmentBit | SwapchainUsageFlags.SampledBit, false);

                _spaceWarpData[i].DepthImages = _xrApp.EnumerateSwapchainImages(_spaceWarpData[i].DepthSwapchain);
            }
        }


        public override void Destroy()
        {
            base.Destroy();

            foreach (var data in _spaceWarpData)
            {
                _xrApp?.DestroySwapchain(data.DepthSwapchain);
                _xrApp?.DestroySwapchain(data.ColorSwapchain);
            }
        }

        protected unsafe override bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, XrSwapchainInfo[] swapchains, long displayTime)
        {
            if (_motionProvider.IsActive != _lastSpaceWarpActive)
            {
                for (var i = 0; i < _spaceWarpInfo.Length; i++)
                {
                    if (_motionProvider.IsActive)
                        StructChain.AddNextStruct(ref projViews[i], _spaceWarpInfo.ItemPointer(i));
                    else
                        projViews[i].Next = null;
                }
                _lastSpaceWarpActive = _motionProvider.IsActive;
            }

            if (!_motionProvider.IsActive)
                return base.Render(ref projViews, ref views, swapchains, displayTime);

            Debug.Assert(_xrApp != null);

            for (var i = 0; i < _spaceWarpData.Length; i++)
            {
                var colorIndex = _xrApp.AcquireSwapchainImage(_spaceWarpData[i].ColorSwapchain);
                _xrApp.WaitSwapchainImage(_spaceWarpData[i].ColorSwapchain);

                var depthIndex = _xrApp.AcquireSwapchainImage(_spaceWarpData[i].DepthSwapchain);
                _xrApp.WaitSwapchainImage(_spaceWarpData[i].DepthSwapchain);

                _spColorImages[i] = _spaceWarpData[i].ColorImages!.ItemPointer((int)colorIndex);
                _spDepthImages[i] = _spaceWarpData[i].DepthImages!.ItemPointer((int)depthIndex);

                _spaceWarpInfo.ItemPointer(i)->LayerFlags = CompositionLayerSpaceWarpInfoFlagsFB.None;
            }

            try
            {
                _motionProvider.UpdateMotionVectors(ref projViews, _spColorImages, _spDepthImages, _xrApp!.RenderOptions.RenderMode);


                for (var i = 0; i < projViews.Length; i++)
                {
                    var info = _spaceWarpInfo.ItemPointer(i);
                    var curPose = _xrApp.ReferenceFrame.Multiply(projViews[i].Pose.ToPose3());
                    var lastPose = _lastPose[i];
                    info->AppSpaceDeltaPose = lastPose.Inverse().Multiply(curPose).ToPoseF(); 
                    _lastPose[i] = curPose; 
                }
     
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
                for (var i = 0; i < _spaceWarpData.Length; i++)
                {
                    _xrApp.ReleaseSwapchainImage(_spaceWarpData[i].ColorSwapchain);
                    _xrApp.ReleaseSwapchainImage(_spaceWarpData[i].DepthSwapchain);
                }
            }

            return true;
        }

        protected unsafe override void UpdateView(ref CompositionLayerProjectionView projView, int index)
        {
            var info = _spaceWarpInfo.ItemPointer(index);

            var dataIndex = _warpTexArray ? 0 : index;
            var arrayIndex = _warpTexArray ? index : 0;

            info->Type = StructureType.CompositionLayerSpaceWarpInfoFB;
            info->Next = null;

            info->DepthSubImage.Swapchain = _spaceWarpData[dataIndex].DepthSwapchain;
            info->DepthSubImage.ImageArrayIndex = (uint)arrayIndex;
            info->DepthSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di { X = 0, Y = 0 },
                Extent = _motionImageSize
            };

            info->MotionVectorSubImage.Swapchain = _spaceWarpData[dataIndex].ColorSwapchain;
            info->MotionVectorSubImage.ImageArrayIndex = (uint)arrayIndex;
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
            foreach (var data in _spaceWarpData)
            {
                data.DepthImages?.Dispose();
                data.ColorImages?.Dispose();
            }

            _spaceWarpInfo.Dispose();

            base.Dispose();
        }
    }
}

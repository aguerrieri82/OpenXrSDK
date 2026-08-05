using Common.Interop;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework.Oculus
{
    public struct SpaceWarpData
    {
        public Extent2Di ColorSize;
        public Extent2Di DepthSize;
        public Swapchain ColorSwapchain;
        public Swapchain DepthSwapchain;
        public NativeArray<SwapchainImageBaseHeader> ColorImages;

        public NativeArray<SwapchainImageBaseHeader> DepthImages;
    }

    public class XrSpaceWarpProjectionLayerV2 : XrProjectionLayer
    {

        readonly NativeArray<CompositionLayerSpaceWarpInfoFB> _spaceWarpInfo;
        readonly IXrMotionVectorProvider _motionProvider;
        readonly Pose3[] _lastPose = new Pose3[2];

        unsafe SwapchainImageBaseHeader* _lastSpColorImage;
        SpaceWarpData _spaceWarpData;
        bool _lastSpaceWarpActive;

        public XrSpaceWarpProjectionLayerV2(RenderViewDelegate renderView, IXrMotionVectorProvider provider)
            : base(renderView, true)
        {
            _spaceWarpInfo = new NativeArray<CompositionLayerSpaceWarpInfoFB>(2, typeof(CompositionLayerSpaceWarpInfoFB));
            _motionProvider = provider;
            _spaceWarpData = new SpaceWarpData();
            _useDepthSWC = true;
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

            _spaceWarpData.ColorSize = new Extent2Di
            {
                Width = (int)spaceWarpProperties.RecommendedMotionVectorImageRectWidth,
                Height = (int)spaceWarpProperties.RecommendedMotionVectorImageRectHeight
            };

            _spaceWarpData.DepthSize = _swapchains![0].DepthSize;

            if (_spaceWarpData.ColorSize.Width == 0 || _spaceWarpData.ColorSize.Height == 0)
                _spaceWarpData.ColorSize = _xrApp.RenderOptions.Size;

            _spaceWarpData.ColorSwapchain = _xrApp.CreateSwapChain(
                          _spaceWarpData.ColorSize,
                          _motionProvider.MotionVectorFormat, // Rgba16f
                          2,
                          SwapchainUsageFlags.ColorAttachmentBit | 
                          SwapchainUsageFlags.SampledBit |
                          SwapchainUsageFlags.UnorderedAccessBit |
                          SwapchainUsageFlags.TransferDstBit |
                          SwapchainUsageFlags.TransferSrcBit);

            _spaceWarpData.ColorImages = _xrApp.EnumerateSwapchainImages(_spaceWarpData.ColorSwapchain);
        }

        public override void Destroy()
        {
            base.Destroy();

            _xrApp?.DestroySwapchain(_spaceWarpData.ColorSwapchain);
        }


        protected unsafe override void Acquire(ref Span<CompositionLayerProjectionView> projViews)
        {
            base.Acquire(ref projViews);

#warning SINGLE VIEW NOT SUPPORTED

            Debug.Assert(_xrApp != null && _lastDepthImages != null && _lastDepthImages.Length == 1);

            var colorIndex = _xrApp.AcquireSwapchainImage(_spaceWarpData.ColorSwapchain);
            _xrApp.WaitSwapchainImage(_spaceWarpData.ColorSwapchain);

            _lastSpColorImage = _spaceWarpData.ColorImages.ItemPointer((int)colorIndex);

            _motionProvider.UpdateMotionVectors(_spaceWarpData, _lastSpColorImage, _lastDepthImages[0], _xrApp.RenderOptions.RenderMode);
        }

        protected override void Release()
        {
            base.Release();

            _xrApp!.ReleaseSwapchainImage(_spaceWarpData.ColorSwapchain);
        }

        protected unsafe override bool Render(ref Span<CompositionLayerProjectionView> projViews, ref View[] views, long displayTime)
        {
            var isActive = _motionProvider.IsActive;

            //isActive = true;

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

            var result = base.Render(ref projViews, ref views, displayTime);

            if (!isActive)
                return result;

            Debug.Assert(_xrApp != null);

            for (var i = 0; i < projViews.Length; i++)
            {
                var info = _spaceWarpInfo.ItemPointer(i);
                info->LayerFlags = CompositionLayerSpaceWarpInfoFlagsFB.None;

                var curPose = _xrApp.ReferenceFrame.Multiply(projViews[i].Pose.ToPose3());
                var lastPose = _lastPose[i];

                info->AppSpaceDeltaPose = lastPose.Inverse().Multiply(curPose).ToPoseF();

                _lastPose[i] = curPose;
            }

            return result;
        }

        protected unsafe override void UpdateView(ref CompositionLayerProjectionView projView, int index)
        {
            var info = _spaceWarpInfo.ItemPointer(index);

            info->Type = StructureType.CompositionLayerSpaceWarpInfoFB;
            info->Next = null;

            var depthIx = _swapchains!.Length == 1 ? 0 : index;

            info->DepthSubImage.Swapchain =  _swapchains[depthIx].DepthSwapchain;
            info->DepthSubImage.ImageArrayIndex = projView.SubImage.ImageArrayIndex;
            info->DepthSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di(0, 0),
                Extent = _swapchains[depthIx].DepthSize
            };

            info->MotionVectorSubImage.Swapchain = _spaceWarpData.ColorSwapchain;
            info->MotionVectorSubImage.ImageArrayIndex = (uint)index;
            info->MotionVectorSubImage.ImageRect = new Rect2Di
            {
                Offset = new Offset2Di { X = 0, Y = 0 },
                Extent = _spaceWarpData.ColorSize
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
            _spaceWarpData.ColorImages?.Dispose();
            _spaceWarpInfo.Dispose();

            base.Dispose();
        }
    }
}

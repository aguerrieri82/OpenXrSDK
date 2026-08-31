using Common.Interop;
using Silk.NET.OpenXR;
using System.Diagnostics;
using XrMath;

namespace OpenXr.Framework.Oculus
{

    public class XrSpaceWarpProjectionLayer : XrProjectionLayer
    {
        protected readonly NativeArray<CompositionLayerSpaceWarpInfoFB> _spaceWarpInfo;
        protected readonly IXrMotionVectorProvider _motionProvider;
        protected readonly Pose3[] _lastPose = new Pose3[2];
        protected bool _lastSpaceWarpActive;
        protected XrSwapchain? _spaceWarpSwap;

        public XrSpaceWarpProjectionLayer(RenderViewDelegate renderView, IXrMotionVectorProvider provider)
            : base(renderView, true)
        {
            _spaceWarpInfo = new NativeArray<CompositionLayerSpaceWarpInfoFB>(2, typeof(CompositionLayerSpaceWarpInfoFB));
            _motionProvider = provider;
            _useDepth = true;
        }

        public override void Create()
        {
            base.Create();

            Debug.Assert(_xrApp != null);

            _spaceWarpSwap = new XrSwapchain(_xrApp);

            var spaceWarpProperties = new SystemSpaceWarpPropertiesFB()
            {
                Type = StructureType.SystemSpaceWarpPropertiesFB
            };

            _xrApp.GetSystemProperties(ref spaceWarpProperties);

            var size = new Extent2Di
            {
                Width = (int)spaceWarpProperties.RecommendedMotionVectorImageRectWidth,
                Height = (int)spaceWarpProperties.RecommendedMotionVectorImageRectHeight
            };

            if (size.Width == 0 || size.Height == 0)
                size = _xrApp.RenderOptions.Size;

            size = AdjustRenderSize(size);

            _spaceWarpSwap.Create(
                          size,
                          _motionProvider.MotionVectorFormat, // Rgba16f
                          2,
                          SwapchainUsageFlags.ColorAttachmentBit |
                          SwapchainUsageFlags.SampledBit |
                          SwapchainUsageFlags.InputAttachmentBitKhr |
                          SwapchainUsageFlags.UnorderedAccessBit |
                          SwapchainUsageFlags.TransferDstBit |
                          SwapchainUsageFlags.TransferSrcBit,
                          SwapchainTarget.MotionVectors);
        }

        public override void Destroy()
        {
            base.Destroy();

            _spaceWarpSwap!.Dispose();
        }

        protected unsafe override void Acquire(ref Span<CompositionLayerProjectionView> projViews)
        {
            base.Acquire(ref projViews);

#warning SINGLE VIEW NOT SUPPORTED

            Debug.Assert(_xrApp != null &&
                        _lastDepthImages != null &&
                        _spaceWarpSwap != null &&
                        _lastDepthImages.Length == 1);

            var colorImage = _spaceWarpSwap.AcquireImageAndWait();

            _motionProvider.UpdateMotionVectors(_spaceWarpSwap, colorImage, _lastDepthImages[0], _xrApp.RenderOptions.RenderMode);
        }

        protected override void Release()
        {
            base.Release();

            _spaceWarpSwap!.Release();
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
            Debug.Assert(_spaceWarpSwap != null &&
                         _depthSwaps != null);

            var info = _spaceWarpInfo.ItemPointer(index);

            info->Type = StructureType.CompositionLayerSpaceWarpInfoFB;
            info->Next = null;

            var depthIx = _depthSwaps.Length == 1 ? 0 : index;
            var depthSwap = _depthSwaps[depthIx];

            info->DepthSubImage.Swapchain = depthSwap;
            info->DepthSubImage.ImageArrayIndex = projView.SubImage.ImageArrayIndex;

            if (UseSimmetricFov)
            {
                var cropW = (int)MathF.Round(depthSwap.Size.Width / _sharedFovScale.X);
                var cropH = (int)MathF.Round(depthSwap.Size.Height / _sharedFovScale.Y);
                var x = index == 0 ? 0 : depthSwap.Size.Width - cropW;

                info->DepthSubImage.ImageRect = new Rect2Di
                {
                    Offset = new Offset2Di(x, 0),
                    Extent = new Extent2Di(cropW, cropH)
                };
            }
            else
            {
                info->DepthSubImage.ImageRect = new Rect2Di
                {
                    Offset = new Offset2Di(0, 0),
                    Extent = depthSwap.Size
                };
            }

            info->MotionVectorSubImage.Swapchain = _spaceWarpSwap;
            info->MotionVectorSubImage.ImageArrayIndex = (uint)index;

            if (UseSimmetricFov)
            {
                var cropW = (int)MathF.Round(_spaceWarpSwap.Size.Width / _sharedFovScale.X);
                var cropH = (int)MathF.Round(_spaceWarpSwap.Size.Height / _sharedFovScale.Y);
                var x = index == 0 ? 0 : _spaceWarpSwap.Size.Width - cropW;

                info->MotionVectorSubImage.ImageRect = new Rect2Di
                {
                    Offset = new Offset2Di(x, 0),
                    Extent = new Extent2Di(cropW, cropH)
                };
            }
            else
            {
                info->MotionVectorSubImage.ImageRect = new Rect2Di
                {
                    Offset = new Offset2Di(0, 0),
                    Extent = _spaceWarpSwap.Size
                };
            }

            info->MaxDepth = 1;
            info->MinDepth = 0;
            info->NearZ = _motionProvider.Near;
            info->FarZ = _motionProvider.Far;
            info->LayerFlags = CompositionLayerSpaceWarpInfoFlagsFB.None;

            info->AppSpaceDeltaPose = new Posef
            {
                Orientation = new Quaternionf { X = 0, Y = 0, Z = 0, W = 1 },
                Position = new Vector3f { X = 0, Y = 0, Z = 0 }
            };
        }

        public override void Dispose()
        {
            _spaceWarpInfo.Dispose();
            _spaceWarpSwap?.Dispose();

            base.Dispose();
        }
    }
}
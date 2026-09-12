using Common.Interop;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public abstract class XrBaseGeometryLayer<T> : XrBaseLayer<T> where T : unmanaged
    {
        protected IGeometryLayerSource _source;
        protected NativeStruct<CompositionLayerDepthTestFB> _depthTest;
        protected NativeStruct<CompositionLayerImageLayoutFB> _layerFlags;

        protected unsafe XrBaseGeometryLayer(IGeometryLayerSource source)
        {
            _source = source;

            _depthTest.Value = new CompositionLayerDepthTestFB
            {
                Type = StructureType.CompositionLayerDepthTestFB,
                DepthMask = 0,
                CompareOp = CompareOpFB.LessOrEqualFB,
                Next = null
            };

            StructChain.AddNextStruct(ref _header.ValueRef, _depthTest.Pointer);

            Priority = XrLayerPriority.BaseQuods;
        }

        public unsafe override void Initialize(XrApp app, IList<string> extensions)
        {
            base.Initialize(app, extensions);

            _source.Initialize(app, extensions);

            if (FlipY)
            {
                extensions.Add("XR_FB_composition_layer_image_layout");

                _layerFlags.Value = new CompositionLayerImageLayoutFB
                {
                    Type = StructureType.CompositionLayerImageLayoutFB,
                    Flags = CompositionLayerImageLayoutFlagsFB.VerticalFlipBitFB,
                    Next = null
                };

                StructChain.AddNextStruct(ref _header.ValueRef, _layerFlags.Pointer);
            }
        }

        public override void Create()
        {
            SetSubImage(ref _header.ValueRef, _source.Create());
        }

        public override void OnBeginFrame(Space space, long displayTime)
        {
            _source.OnBeginFrame(space, displayTime);
        }

        protected override bool Update(ref T layer, ref View[] views, long predTime)
        {
            if (!_source.Update(predTime))
                return false;

            return UpdateGeometry(ref layer, ref views, predTime);
        }

        protected abstract void SetSubImage(ref T layer, SwapchainSubImage subImage);

        protected abstract bool UpdateGeometry(ref T layer, ref View[] views, long predTime);

        public override void OnEndFrame()
        {
            _source.OnEndFrame();
        }

        public override void Destroy()
        {
            _source.Destroy();
            _layerFlags.Dispose();
            _depthTest.Dispose();

            base.Destroy();
        }

        public bool FlipY { get; set; }
    }
}
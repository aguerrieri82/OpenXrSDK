using Common.Interop;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using XrMath;

namespace OpenXr.Framework
{

    public delegate Quad3 GetQuadDelegate();

    public abstract class XrBaseQuadLayer : XrBaseLayer<CompositionLayerQuad>
    {

        protected GetQuadDelegate _getQuad;
        protected NativeStruct<CompositionLayerDepthTestFB> _depthTest;


        public unsafe XrBaseQuadLayer(GetQuadDelegate getQuad)
        {
            _getQuad = getQuad;
            _header.ValueRef.Type = StructureType.CompositionLayerQuad;

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

        protected override unsafe bool Update(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            var quad = _getQuad();
            var pose = quad.Pose;

            layer.Size.Width = quad.Size.X;
            layer.Size.Height = quad.Size.Y;
            layer.Pose = _xrApp!.ReferenceFrame.Inverse().Multiply(quad.Pose).ToPoseF();

            return true;
        }

    }
}

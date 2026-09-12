using Silk.NET.OpenXR;
using XrMath;

namespace OpenXr.Framework
{
    public delegate Quad3 GetQuadDelegate();

    public class XrQuadLayer : XrBaseGeometryLayer<CompositionLayerQuad>
    {
        protected GetQuadDelegate _getQuad;

        public XrQuadLayer(GetQuadDelegate getQuad, IGeometryLayerSource source)
            : base(source)
        {
            _getQuad = getQuad;

            _header.ValueRef.Type = StructureType.CompositionLayerQuad;
        }

        protected override void SetSubImage(ref CompositionLayerQuad layer, SwapchainSubImage subImage)
        {
            layer.SubImage = subImage;
            layer.EyeVisibility = EyeVisibility.Both;
            layer.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }

        protected override bool UpdateGeometry(ref CompositionLayerQuad layer, ref View[] views, long predTime)
        {
            var quad = _getQuad();

            layer.Size.Width = quad.Size.X;
            layer.Size.Height = quad.Size.Y;
            layer.Pose = _xrApp!.ReferenceFrame.Inverse().Multiply(quad.Pose).ToPoseF();

            return true;
        }
    }
}
using Silk.NET.OpenXR;
using XrMath;
using XrMath.Entities;

namespace OpenXr.Framework
{
    public class XrEquirectLayer : XrBaseGeometryLayer<CompositionLayerEquirectKHR>
    {
        protected GetSphericalSectionDelegate _getSection;

        public XrEquirectLayer(GetSphericalSectionDelegate getSection, IGeometryLayerSource source)
            : base(source)
        {
            _getSection = getSection;

            _header.ValueRef.Type = StructureType.CompositionLayerEquirectKhr;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            extensions.Add("XR_KHR_composition_layer_equirect");

            base.Initialize(app, extensions);
        }

        protected override void SetSubImage(ref CompositionLayerEquirectKHR layer, SwapchainSubImage subImage)
        {
            layer.SubImage = subImage;
            layer.EyeVisibility = EyeVisibility.Both;
            layer.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }

        protected override bool UpdateGeometry(ref CompositionLayerEquirectKHR layer, ref View[] views, long predTime)
        {
            var section = _getSection();

            var horizontalScale = MathF.Tau / section.HorizontalAngle;
            var verticalAngle = section.UpperVerticalAngle - section.LowerVerticalAngle;
            var verticalScale = MathF.PI / verticalAngle;

            layer.Pose = _xrApp!.ReferenceFrame.Inverse().Multiply(section.Pose).ToPoseF();
            layer.Radius = section.Radius;

            layer.Scale = new Vector2f(horizontalScale, verticalScale);

            layer.Bias = new Vector2f(
                0.5f - horizontalScale * 0.5f,
                0.5f - verticalScale * 0.5f);

            return true;
        }
    }
}
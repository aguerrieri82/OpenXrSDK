using Silk.NET.OpenXR;
using XrMath;
using XrMath.Entities;

namespace OpenXr.Framework
{
    public delegate SphericalSection GetSphericalSectionDelegate();

    public class XrEquirect2Layer : XrBaseGeometryLayer<CompositionLayerEquirect2KHR>
    {
        protected GetSphericalSectionDelegate _getSection;

        public XrEquirect2Layer(GetSphericalSectionDelegate getSection, IGeometryLayerSource source)
            : base(source)
        {
            _getSection = getSection;

            _header.ValueRef.Type = StructureType.CompositionLayerEquirect2Khr;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            extensions.Add("XR_KHR_composition_layer_equirect2");

            base.Initialize(app, extensions);
        }

        protected override void SetSubImage(ref CompositionLayerEquirect2KHR layer, SwapchainSubImage subImage)
        {
            layer.SubImage = subImage;
            layer.EyeVisibility = EyeVisibility.Both;
            layer.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }

        protected override bool UpdateGeometry(ref CompositionLayerEquirect2KHR layer, ref View[] views, long predTime)
        {
            var section = _getSection();

            layer.Pose = _xrApp!.ReferenceFrame.Inverse().Multiply(section.Pose).ToPoseF();
            layer.Radius = section.Radius;
            layer.CentralHorizontalAngle = section.HorizontalAngle;
            layer.UpperVerticalAngle = section.UpperVerticalAngle;
            layer.LowerVerticalAngle = section.LowerVerticalAngle;

            return true;
        }
    }
}
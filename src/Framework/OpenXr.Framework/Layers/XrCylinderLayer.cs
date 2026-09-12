using Silk.NET.OpenXR;
using XrMath;

namespace OpenXr.Framework
{
    public delegate Cylinder GetCylinderDelegate();

    public class XrCylinderLayer : XrBaseGeometryLayer<CompositionLayerCylinderKHR>
    {
        protected GetCylinderDelegate _getCylinder;

        public XrCylinderLayer(GetCylinderDelegate getCylinder, IGeometryLayerSource source)
            : base(source)
        {
            _getCylinder = getCylinder;

            _header.ValueRef.Type = StructureType.CompositionLayerCylinderKhr;
        }

        public override void Initialize(XrApp app, IList<string> extensions)
        {
            extensions.Add("XR_KHR_composition_layer_cylinder");

            base.Initialize(app, extensions);
        }

        protected override void SetSubImage(ref CompositionLayerCylinderKHR layer, SwapchainSubImage subImage)
        {
            layer.SubImage = subImage;
            layer.EyeVisibility = EyeVisibility.Both;
            layer.LayerFlags = CompositionLayerFlags.BlendTextureSourceAlphaBit;
        }

        protected override bool UpdateGeometry(ref CompositionLayerCylinderKHR layer, ref View[] views, long predTime)
        {
            var cylinder = _getCylinder();

            layer.Pose = _xrApp!.ReferenceFrame.Inverse().Multiply(cylinder.Pose).ToPoseF();
            layer.Radius = cylinder.Radius;
            layer.CentralAngle = cylinder.Angle;
            layer.AspectRatio = cylinder.Radius * cylinder.Angle / cylinder.Height;

            return true;
        }
    }
}
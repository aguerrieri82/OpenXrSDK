using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using XrEngine.OpenGL;
using XrEngine.OpenXr.Components;
using XrMath;

namespace XrEngine.OpenXr
{
    public class XrScreenAttached : BaseXrLayerAttach<CurvedScreen, XrCylinderLayer>
    {

        public XrScreenAttached(Texture2D texture)
        {
            Initialize(texture);
        }

        public XrScreenAttached(IGeometryLayerSource source)
        {
            _source = source;
        }

        protected override XrCylinderLayer CreateLayer()
        {
            Debug.Assert(_source != null);

            return new XrCylinderLayer(GetCylinder, _source)
            {
                Priority = XrLayerPriority.UiGeomeytry,
                FlipY = FlipY && SupportNativeFlip
            };
        }

        private Cylinder GetCylinder()
        {
            Debug.Assert(_host != null);

            var radius = _host.Width / _host.Curvature;
            var orientation = _host.WorldOrientation;

            var center = _host.WorldPosition +
                Vector3.Transform(Vector3.UnitZ * radius, orientation);

            return new Cylinder
            {
                Pose = new Pose3
                {
                    Position = center,
                    Orientation = orientation
                },
                Radius = radius,
                Height = _host.Height,
                Angle = _host.Curvature
            };
        }
    }
}
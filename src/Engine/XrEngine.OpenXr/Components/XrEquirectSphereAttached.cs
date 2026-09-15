using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using XrEngine.OpenGL;
using XrEngine.OpenXr.Components;
using XrMath;
using XrMath.Entities;

namespace XrEngine.OpenXr
{
    public class XrEquirectSphereAttached : BaseXrLayerAttach<EquirectSphere, IXrLayer>
    {

        public XrEquirectSphereAttached(Texture2D texture)
        {
            Initialize(texture);

            _textureMaterial!.CullFront = true; 
        }

        public XrEquirectSphereAttached(IGeometryLayerSource source)
        {
            _source = source;
        }

        protected override IXrLayer CreateLayer()
        {
            Debug.Assert(_source != null);

            if (_xrApp!.HasExtension("XR_KHR_composition_layer_equirect2") && !OperatingSystem.IsWindows())
            {
                return new XrEquirect2Layer(GetSection, _source)
                {
                    Priority = XrLayerPriority.BaseGeometry,
                    FlipY = FlipY && SupportNativeFlip
                };
            }
           
            if (_xrApp!.HasExtension("XR_KHR_composition_layer_equirect"))
            {
                return new XrEquirectLayer(GetSection, _source)
                {
                    Priority = XrLayerPriority.BaseGeometry,
                    FlipY = FlipY && SupportNativeFlip
                };
            }

            throw new NotSupportedException();
        }

        private SphericalSection GetSection()
        {
            Debug.Assert(_host != null);

            var orientation = Quaternion.Normalize(
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2) *
                _host.WorldOrientation);

            return new SphericalSection
            {
                Pose = new Pose3
                {
                    Position = _host.WorldPosition,
                    Orientation = orientation
                },
                Radius = _host.Radius,
                HorizontalAngle = MathF.Tau * _host.Section,
                UpperVerticalAngle = MathF.PI / 2,
                LowerVerticalAngle = -MathF.PI / 2
            };
        }

    }
}
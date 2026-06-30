using XrMath;

namespace XrEngine
{
    public class LightViewer : Behavior<Light>
    {
        private GlowSphere? _sphere;
        private GlowClone? _cone;
        private long _lastUpdateVersion;

        protected override void Update(RenderContext ctx)
        {
            if (_host is PointLight point)
            {
                _sphere ??= new GlowSphere();

                if (_sphere.Parent == null)
                    _host.Scene!.AddChild(_sphere);

                _sphere.WorldPosition = _host.WorldPosition;
                _sphere.Transform.SetScale(point.Range * 2);
                _sphere.IsVisible = _host.IsVisible;

                var mat = (GlowSphereMaterial)_sphere.Materials[0];
                mat.Intensity = point.Intensity;
                mat.Width = point.Range * 0.2f;
                mat.Radius = point.Range - mat.Width;
                mat.Color = new Color(point.Color.R, point.Color.G, point.Color.B, 1f);

                var version = point.Version + point.ContentVersion;
                if (version != _lastUpdateVersion)
                    mat.Invalidate();

                _lastUpdateVersion = version;
            }
            else if (_host is SpotLight spot)
            {
                _cone ??= new GlowClone();

                if (_cone.Parent == null)
                    _host.Scene!.AddChild(_cone);

                var dir = _host.Forward;
                var farRadius = MathF.Tan(spot.OuterConeAngle) * spot.Range;

                _cone.WorldPosition = _host.WorldPosition + dir * (spot.Range * 0.5f);
                _cone.Direction = dir;
                _cone.Transform.SetScale(farRadius * 2f, spot.Range, 1f);
                _cone.IsVisible = _host.IsVisible;

                var mat = (GlowConeMaterial)_cone.Materials[0];
                mat.Intensity = spot.Intensity;
                mat.Range = spot.Range;
                mat.InnerAngle = spot.InnerConeAngle;
                mat.OuterAngle = spot.OuterConeAngle;
                mat.Color = new Color(spot.Color.R, spot.Color.G, spot.Color.B, 1f);

                var version = spot.Version + spot.ContentVersion;
                if (version != _lastUpdateVersion)
                    mat.Invalidate();

                _lastUpdateVersion = version;
            }

            base.Update(ctx);
        }
    }
}
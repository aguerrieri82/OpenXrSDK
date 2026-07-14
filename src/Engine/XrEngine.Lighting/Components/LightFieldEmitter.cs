using System;
using System.Collections.Generic;
using System.Text;
using XrMath;

namespace XrEngine.Lighting
{
    public class LightFieldEmitter : BaseComponent<Light>
    {
        private LightContribution? _contrib;
        private long _lightVersion;

        public LightFieldEmitter()
        {
            _lightVersion = -1;
        }

        public void UpdateLight(VoxelLightBaker backer)
        {
            if (_host is PointLight point)
            {
                _contrib = backer.BakeLight(new VoxPointLight
                {
                    Color = point.Color.ToVector3(),
                    Falloff = new LightCurve
                    {
                        Factor = 1,
                        Range = point.Range,
                        Type = LightCurveType.Quadratic
                    },
                    Intensity = point.Intensity,
                    Position = point.WorldPosition
                });
            }

            else if (_host is DirectionalLight dir)
            {
                _contrib = backer.BakeLight(new VoxDirectionalLight
                {
                    Color = dir.Color.ToVector3(),
                    Falloff = new LightCurve
                    {
                        Factor = 1,
                        Range = 100,
                        Type = LightCurveType.Quadratic
                    },
                    Direction = dir.Direction,
                    Position = dir.WorldPosition,
                    Intensity = dir.Intensity,
                });
            }

            else if (_host is SpotLight spot)
            {
                _contrib = backer.BakeLight(new VoxSpotLight
                {
                    Color = spot.Color.ToVector3(),
                    Falloff = new LightCurve
                    {
                        Factor = 1,
                        Range = spot.Range,
                        Type = LightCurveType.Quadratic
                    },
                    InnerCos = MathF.Acos(spot.InnerConeAngle),
                    OuterCos = MathF.Acos(spot.OuterConeAngle),
                    Intensity = spot.Intensity, 
                    Direction = spot.Direction,
                    Position = spot.WorldPosition,
                });
            }
            else
                throw new NotSupportedException();

            _lightVersion = _host.ContentVersion + _host.Version;
        }


        public LightContribution? Contributions => _contrib;

        public bool NeedUpdate => _host != null && _host.ContentVersion + _host.Version != _lightVersion;
    }
}

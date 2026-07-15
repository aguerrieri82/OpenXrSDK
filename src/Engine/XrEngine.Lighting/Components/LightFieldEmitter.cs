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
        private LightFieldProvider? _provider;

        public LightFieldEmitter()
        {
            _lightVersion = -1;
        }

        [Action]
        public void ForceUpdate()
        {
            _host!.Invalidate();
            
            _ = _provider?.RebuildAsync();
        }

        public void UpdateLight(LightFieldProvider provider)
        {
            _provider = provider;

            UpdateLight(provider.Baker);
        }

        public void UpdateLight(VoxelLightBaker backer)
        {
            Log.Info(this, "Updating light {0}", _host!.Name ?? _host.GetType().Name);

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
            else if (_host is AreaLight area)
            {
                _contrib = backer.BakeLight(new VoxAreaLight
                {
                    Color = area.Color.ToVector3(),
                    Falloff = new LightCurve
                    {
                        Factor = 1,
                        Range = area.Range,
                        Type = LightCurveType.Quadratic
                    },
                    Intensity = area.Intensity,
                    Direction = area.Direction,
                    Position = area.WorldPosition,
                    Height = area.PlaneSize.Y,
                    Width = area.PlaneSize.X,
                    Normal = area.PlaneNormal,
                    Up = area.PlaneUp,
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
                    InnerCos = MathF.Cos(spot.InnerConeAngle),
                    OuterCos = MathF.Cos(spot.OuterConeAngle),
                    Intensity = spot.Intensity, 
                    Direction = spot.Direction,
                    Position = spot.WorldPosition,
                });
            }
        
            _lightVersion = _host!.ContentVersion + _host.Version;

            Log.Debug(this, "Light updated");
        }


        public LightContribution? Contributions => _contrib;

        public bool NeedUpdate => _host != null && _host.ContentVersion + _host.Version != _lightVersion;
    }
}

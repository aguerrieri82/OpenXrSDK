using XrMath;

namespace XrEngine.Lighting
{
    public class LightFieldEmitter : BaseComponent<Light>, IDisposable
    {
        private LightContribution? _contrib;
        private LightContributionV2? _contribV2;
        private long _lightVersion;
        private long _lightVersionV2;
        private LightFieldProvider? _provider;

        public LightFieldEmitter()
        {
            _lightVersion = -1;
            _lightVersionV2 = -1;
        }

        [Action]
        public void ForceUpdate()
        {
            _host.Invalidate();

            _ = _provider?.RebuildAsync();
        }

        public void UpdateLight(LightFieldProvider provider)
        {
            _provider = provider;
            UpdateLight(provider.Baker);
        }

        public void UpdateLight(VoxelLightBaker backer)
        {
            Log.Info(this, "Updating light {0}", _host.Name ?? _host.GetType().Name);

            _contrib?.Dispose();
            _contrib = null;

            if (_host is PointLight point)
                _contrib = backer.BakeLight(ToVoxelLight(point));
            else if (_host is AreaLight area)
                _contrib = backer.BakeLight(ToVoxelLight(area));
            else if (_host is DirectionalLight dir)
                _contrib = backer.BakeLight(ToVoxelLight(dir));
            else if (_host is SpotLight spot)
                _contrib = backer.BakeLight(ToVoxelLight(spot));

            _lightVersion = LightVersion;

            Log.Debug(this, "Light updated");
        }

        public void UpdateLight(VoxelLightBakerV2 backer)
        {
            Log.Info(this, "Updating V2 light {0}", _host.Name ?? _host.GetType().Name);

            _contribV2 ??= new LightContributionV2();

            if (_host is PointLight point)
                backer.BakeLight(ToVoxelLight(point), _contribV2);
            else if (_host is AreaLight area)
                backer.BakeLight(ToVoxelLight(area), _contribV2);
            else if (_host is DirectionalLight dir)
                backer.BakeLight(ToVoxelLight(dir), _contribV2);
            else if (_host is SpotLight spot)
                backer.BakeLight(ToVoxelLight(spot), _contribV2);

            _lightVersionV2 = LightVersion;

            Log.Debug(this, "V2 light updated");
        }

        public void InvalidateV2()
        {
            _lightVersionV2 = -1;
        }

        private static VoxPointLight ToVoxelLight(PointLight point)
        {
            return new VoxPointLight
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
            };
        }

        private static VoxAreaLight ToVoxelLight(AreaLight area)
        {
            return new VoxAreaLight
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
                Up = area.PlaneUp
            };
        }

        private static VoxDirectionalLight ToVoxelLight(DirectionalLight dir)
        {
            return new VoxDirectionalLight
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
                Intensity = dir.Intensity
            };
        }

        private static VoxSpotLight ToVoxelLight(SpotLight spot)
        {
            return new VoxSpotLight
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
                Position = spot.WorldPosition
            };
        }

        private long LightVersion => _host.ContentVersion + _host.Version;

        public LightContribution? Contributions => _contrib;

        public LightContributionV2? ContributionsV2 => _contribV2;

        public bool NeedUpdate => _host != null && LightVersion != _lightVersion;

        public bool NeedUpdateV2 => _host != null && LightVersion != _lightVersionV2;

        public void Dispose()
        {
            _contrib?.Dispose();
            _contrib = null;

            _contribV2?.Dispose();
            _contribV2 = null;
        }
    }
}

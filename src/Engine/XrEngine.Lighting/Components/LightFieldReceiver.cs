using System.Diagnostics;

namespace XrEngine.Lighting
{
    public class LightFieldReceiver : BaseComponent<TriangleMesh>
    {
        protected GpuVoxelFaceData[]? _voxels;
        protected long _voxelsVersion;

        public LightFieldReceiver()
        {
            IsOccluder = true;
            _voxelsVersion = -1;
        }

        public void UpdateVoxels(IMeshVoxelizer voxelizer)
        {
            Log.Info(this, "Voxelize {0}", _host.Name ?? _host.GetType().Name);

            Debug.Assert(_host != null);

            _voxels = voxelizer.Voxelize([_host]).ToArray();

            _voxelsVersion = _host.Version;

            Log.Debug(this, "Voxelize done");
        }

        public bool NeedUpdate => IsOccluder && _voxelsVersion != _host?.Version;

        public GpuVoxelFaceData[]? Voxels => _voxels;

        public bool IsOccluder { get; set; }
    }
}

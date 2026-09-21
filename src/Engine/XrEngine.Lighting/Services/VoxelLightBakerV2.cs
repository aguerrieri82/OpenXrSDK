using Common.Interop;
using System.Diagnostics;
using XrMath;

namespace XrEngine.Lighting
{
    public sealed unsafe class LightContributionV2 : IDisposable
    {
        internal VoxelLightContributionViewV2 View;

        public Span<VoxelLightSample> Samples => View.SampleCount == 0 ? [] : new Span<VoxelLightSample>(View.Samples, View.SampleCount);

        public int Count => View.SampleCount;

        public void Dispose()
        {
            if (View.Samples == null)
                return;

            EngineNativeLibV2.FreeContributionViewV2(ref View);
            View = default;
        }
    }


    public sealed unsafe class VoxelLightBakerV2 : IDisposable
    {
        private EngineNativeLibV2.VoxelLightBakerV2 _handle;
        private VoxelLightFieldViewV2 _view;
        private VoxelGridDesc _gridDesc;
        private VoxelLightBakeParamsV2 _params;
        private bool _fieldValid;

        public VoxelLightBakerV2()
        {
            _handle = EngineNativeLibV2.VoxelLightBakerV2Create();
        }

        public int CellIndex(Vector3I v) => CellIndex(v.X, v.Y, v.Z);

        public int CellIndex(int x, int y, int z)
        {
            var size = _gridDesc.Size;

            Debug.Assert((uint)x < (uint)size.X);
            Debug.Assert((uint)y < (uint)size.Y);
            Debug.Assert((uint)z < (uint)size.Z);

            return x + y * size.X + z * size.X * size.Y;
        }

        public ref T GetCell<T>(Span<T> cells, Vector3I index) => ref cells[CellIndex(index)];

        public ref T GetCell<T>(Span<T> cells, int x, int y, int z) => ref cells[CellIndex(x, y, z)];

        public Span<VoxelData> GetScene()
        {
            var sceneRef = EngineNativeLibV2.VoxelLightBakerV2GetScene(_handle, out var count);

            if (count == 0)
                return [];

            return new Span<VoxelData>(sceneRef, count);
        }

        public void SetParams(in VoxelLightBakeParamsV2 parameters)
        {
            var value = parameters;

            EngineNativeLibV2.VoxelLightBakerV2SetParams(_handle, ref value);

            _params = parameters;
            _fieldValid = false;
        }

        public void SetGrid(in VoxelGridDesc grid)
        {
            var value = grid;

            EngineNativeLibV2.VoxelLightBakerV2SetGrid(_handle, ref value);

            _gridDesc = grid;
            _fieldValid = false;
        }

        public void ClearScene()
        {
            EngineNativeLibV2.VoxelLightBakerV2ClearScene(_handle);
        }

        public void AddMesh(GpuVoxelFaceData[] faces)
        {
            EngineNativeLibV2.VoxelLightBakerV2AddGpuMeshFaces(_handle, faces, faces.Length);
        }

        public void AddMesh(in Vector3I origin, in Vector3I size, VoxelData[] voxels, VoxelMeshResolvedFace[] faces)
        {
            var originValue = origin;
            var sizeValue = size;

            EngineNativeLibV2.VoxelLightBakerV2AddMesh(_handle, ref originValue, ref sizeValue, voxels, faces, faces.Length);
        }

        public LightContributionV2 BakeLight(in VoxPointLight light)
        {
            var lightValue = light;
            var result = new LightContributionV2();

            EngineNativeLibV2.VoxelLightBakerV2BakePointLight(_handle, ref lightValue, ref result.View);

            return result;
        }

        public LightContributionV2 BakeLight(in VoxDirectionalLight light)
        {
            var lightValue = light;
            var result = new LightContributionV2();

            EngineNativeLibV2.VoxelLightBakerV2BakeDirectionalLight(_handle, ref lightValue, ref result.View);

            return result;
        }

        public LightContributionV2 BakeLight(in VoxSpotLight light)
        {
            var lightValue = light;
            var result = new LightContributionV2();

            EngineNativeLibV2.VoxelLightBakerV2BakeSpotLight(_handle, ref lightValue, ref result.View);

            return result;
        }

        public LightContributionV2 BakeLight(in VoxAreaLight light)
        {
            var lightValue = light;
            var result = new LightContributionV2();

            EngineNativeLibV2.VoxelLightBakerV2BakeAreaLight(_handle, ref lightValue, ref result.View);

            return result;
        }

        public void ClearLightField()
        {
            EngineNativeLibV2.VoxelLightBakerV2ClearLightField(_handle);
            _fieldValid = false;
        }

        public void AccumulateLight(LightContributionV2 contribution)
        {
            EngineNativeLibV2.VoxelLightBakerV2AccumulateLight(_handle, ref contribution.View);

            _fieldValid = false;
        }

        public VoxelLightFieldViewV2 GetLightField(bool update = false)
        {
            if (!_fieldValid || update)
            {
                EngineNativeLibV2.VoxelLightBakerV2GetLightField(_handle, ref _view);

                _fieldValid = true;
            }

            return _view;
        }

        public VoxelLightFieldViewV2 BuildLightField(float angularTolerance, float relativeEnergyTolerance)
        {
            EngineNativeLibV2.VoxelLightBakerV2BuildLightField(_handle, angularTolerance, relativeEnergyTolerance, ref _view);

            _fieldValid = true;

            return _view;
        }

        public Span<VoxelLightLookup> GetLookup(bool update = false)
        {
            var field = GetLightField(update);

            if (field.LookupCount == 0)
                return [];

            return new Span<VoxelLightLookup>(field.Lookup, field.LookupCount);
        }

        public Span<VoxelLightGpuContribution> GetContributions(bool update = false)
        {
            var field = GetLightField(update);

            if (field.ContributionCount == 0)
                return [];

            return new Span<VoxelLightGpuContribution>(field.Contributions, field.ContributionCount);
        }

        public Texture3D CreateLookupTexture(TextureFormat format)
        {
            var field = GetLightField(false);

            var texture = new Texture3D
            {
                Format = format,
                MipLevelCount = 0,
                MinFilter = ScaleFilter.Nearest,
                MagFilter = ScaleFilter.Nearest,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                WrapR = WrapMode.ClampToEdge
            };

            if (field.LookupCount == 0)
                return texture;

            var size = checked((uint)(field.LookupCount * sizeof(VoxelLightLookup)));

            texture.LoadData(new TextureData
            {
                Content = MemoryBuffer.Attach((byte*)field.Lookup, size),
                Width = (uint)field.Size.X,
                Height = (uint)field.Size.Y,
                Depth = (uint)field.Size.Z,
                Format = format
            });

            return texture;
        }

        public Span<VoxelLightLookup> Lookup => _view.LookupCount == 0 ? [] : 
            new Span<VoxelLightLookup>(_view.Lookup, _view.LookupCount);

        public Span<VoxelLightGpuContribution> Contributions => _view.ContributionCount == 0 ? [] : 
            new Span<VoxelLightGpuContribution>(_view.Contributions, _view.ContributionCount);

        public void Dispose()
        {
            if (_handle.Handle == 0)
                return;

            if (_view.Lookup != null || _view.Contributions != null)
                EngineNativeLibV2.FreeLightFieldViewV2(ref _view);

            _view = default;

            EngineNativeLibV2.VoxelLightBakerV2Destroy(_handle);

            _handle = default;
        }

        public VoxelLightBakeParamsV2 Params => _params;

        public VoxelGridDesc GridDesc => _gridDesc;

        internal EngineNativeLibV2.VoxelLightBakerV2 Handle => _handle;
    }
}

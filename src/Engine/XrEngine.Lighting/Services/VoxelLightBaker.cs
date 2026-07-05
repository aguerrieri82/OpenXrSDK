using Common.Interop;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using XrMath;

namespace XrEngine.Lighting
{
    public sealed unsafe class VoxelLightBaker : IDisposable
    {
        private EngineNativeLib.VoxelLightBaker _handle;

        private VoxelLightFieldView _view;
        private VoxelGridDesc _gridDesc;

        public VoxelLightBaker()
        {
            _handle = EngineNativeLib.VoxelLightBakerCreate();
        }

        public int CellIndex(Vector3I v) => CellIndex(v.X, v.Y, v.Z);

        public int CellIndex(int x, int y, int z)
        {
            var size = _gridDesc.Size;

            Debug.Assert((uint)x < (uint)size.X);
            Debug.Assert((uint)y < (uint)size.Y);
            Debug.Assert((uint)z < (uint)size.Z);

            return
                x +
                y * size.X +
                z * size.X * size.Y;
        }

        public ref T GetCell<T>(Span<T> cells, Vector3I index) =>
            ref cells[CellIndex(index)];

        public ref T GetCell<T>(Span<T> cells, int x, int y, int z) 
            => ref cells[CellIndex(x, y, z)];

        public Span<VoxelData> GetScene()
        {
            var sceneRef = EngineNativeLib.VoxelLightBakerGetScene(_handle, out var count);

            if (count == 0)
                return [];

            return new Span<VoxelData>(sceneRef, count);
        }

        public void SetParams(in VoxelLightBakeParams parameters)
        {
            var value = parameters;

            EngineNativeLib.VoxelLightBakerSetParams(
                _handle,
                ref value);
        }

        public void SetGrid(in VoxelGridDesc grid)
        {
            var value = grid;

            EngineNativeLib.VoxelLightBakerSetGrid(
                _handle,
                ref value);

            _gridDesc = grid;
        }

        public void ClearScene()
        {
            EngineNativeLib.VoxelLightBakerClearScene(_handle);
        }

        public void AddMesh(GpuVoxelFaceData[] faces)
        {
            EngineNativeLib.VoxelLightBakerAddGpuMeshFaces(
                _handle,
                faces,
                faces.Length);
        }

        public void AddMesh(
            in Vector3I origin,
            in Vector3I size,
            VoxelData[] voxels,
            VoxelMeshResolvedFace[] faces)
        {
            var originValue = origin;
            var sizeValue = size;

            EngineNativeLib.VoxelLightBakerAddMesh(
                _handle,
                ref originValue,
                ref sizeValue,
                voxels,
                faces,
                faces.Length);
        }

        public VoxelLightCell[] BakePointLight(in VoxPointLight light)
        {
            var lightValue = light;
            var view = new VoxelLightContributionView();

            var count = EngineNativeLib.VoxelLightBakerBakePointLight(
                _handle,
                ref lightValue,
                ref view);

            if (count <= 0)
                return [];

            var cells = new VoxelLightCell[count];

            fixed (VoxelLightCell* pCells = cells)
            {
                view.Cells = pCells;
                view.CellCount = cells.Length;

                EngineNativeLib.VoxelLightBakerBakePointLight(
                    _handle,
                    ref lightValue,
                    ref view);
            }

            return cells;
        }

        public void ClearLightField()
        {
            EngineNativeLib.VoxelLightBakerClearLightField(_handle);
        }

        public void AccumulateLight(VoxelLightCell[] cells)
        {
            if (cells.Length == 0)
                return;

            fixed (VoxelLightCell* pCells = cells)
            {
                var view = new VoxelLightContributionView
                {
                    Cells = pCells,
                    CellCount = cells.Length
                };

                EngineNativeLib.VoxelLightBakerAccumulateLight(
                    _handle,
                    ref view);
            }
        }

        public IList<Texture3D> CreateTextures()
        {
            var field = GetLightField(false);

            var result = new List<Texture3D>();
            
            var size = (uint)(field.CellCapacity * sizeof(Vector3));

            for (var i = 0; i < 6; i++)
            {
                var tex = new Texture3D();
                tex.Format = TextureFormat.RgbFloat16;

                var span = new Span<Vector3>((Vector*)field.Color[i], field.CellCapacity);

                tex.LoadData(new TextureData
                {
                    Data = MemoryBuffer.Attach((byte*)field.Color[i],size),
                    Width = (uint)field.Size.X,
                    Height = (uint)field.Size.Y,
                    Depth = (uint)field.Size.Z,
                    Format = TextureFormat.RgbFloat32
                });

                result.Add(tex);

                tex = new Texture3D();
                tex.Format = TextureFormat.RgbFloat16;

                tex.LoadData(new TextureData
                {
                    Data = MemoryBuffer.Attach((byte*)field.Direction[i], size),
                    Width = (uint)field.Size.X,
                    Height = (uint)field.Size.Y,
                    Depth = (uint)field.Size.Z,
                    Format = TextureFormat.RgbFloat32
                });

                result.Add(tex);
            }

            return result;
        }

        public VoxelLightFieldView GetLightField(bool update)
        {
            if (_view.CellCount == 0 || update)
            {
                EngineNativeLib.VoxelLightBakerGetLightField(
                _handle,
                ref _view);
            }

            return _view;
        }


        public VoxelRayMarcher CreateRayMarcher()
        {
            return new VoxelRayMarcher(this);
        }


        public void Dispose()
        {
            if (_handle.Handle == 0)
                return;

            if (_view.CellCapacity > 0)
                EngineNativeLib.FreeLightFieldView(ref _view);

            _view.CellCapacity = 0;

            EngineNativeLib.VoxelLightBakerDestroy(_handle);

            _handle = default;
        }

        public VoxelGridDesc GridDesc => _gridDesc;
        internal EngineNativeLib.VoxelLightBaker Handle => _handle;
    }
}
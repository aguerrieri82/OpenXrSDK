using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine.Lighting
{
    public class MeshVoxelizerManager : BaseComponent<TriangleMesh>
    {
        MeshVoxelizer _voxelizer;
        VoxelMeshView? _view;
        VoxelLightBaker _backer;

        private MeshVoxelGrid? _voxelGrid;
        private VoxelRayMarcher? _ray;
        private TriangleMesh _curVoxel;

        public MeshVoxelizerManager(VoxelGridDesc gridDesc)
        {
            _backer = new VoxelLightBaker();

            _voxelizer = new MeshVoxelizer
            {
                GridDesc = gridDesc,
                GridParams = new()
                {
                    ScanSubdiv = 2
                }
            };

            _curVoxel = new TriangleMesh(Cube3D.Default, new ColorMaterial(Color.White));
            _curVoxel.Transform.SetScale(gridDesc.VoxelSize);
        }


        [Action]
        public void Apply()
        {
            Log.Info(this, "Begin");

            _voxelGrid = _voxelizer.Voxelize(_host!.Geometry!, _host.WorldMatrix);

            _view ??= new VoxelMeshView();

            _view.SetTarget(_host, _voxelGrid, _voxelizer.GridDesc);

            if (_view.Parent == null)
                _host!.Scene!.AddChild(_view);

            Log.Info(this, "Done");
        }


        [Action]
        public void Backe()
        {
            _ray?.Dispose();

            _backer.ClearLightField();
            _backer.ClearScene();
            _backer.SetGrid(_voxelizer.GridDesc);
            _backer.SetParams(new VoxelLightBakeParams
            {
                RaySpacingFactor = 1.0f,
                EmptyDissipation = 0.15f,
                EnergyThreshold = 0.001f,

                MaxBounceCount = 4,
                ThreadCount = 0,
            });

            var resolved = _view!.ResolvedFaces();

            if (resolved == null)
                return;

            var resInfo = new VoxelMeshResolvedFace[resolved!.Length];

            for (var i = 0; i < _view.FaceInstances!.Length; i++)
            {
                var pos = _view.FaceInstances[i].Pos - _voxelGrid!.Info.Origin;

                var voxelIndex =
                    pos.X +
                    pos.Y * _voxelGrid.Info.Size.X +
                    pos.Z * _voxelGrid.Info.Size.X * _voxelGrid.Info.Size.Y;

                resInfo[i] = new VoxelMeshResolvedFace
                {
                    Resolved = resolved[i],
                    Data = _view.FaceInstances[i].Data,
                    Face = _view.FaceInstances[i].Face,
                    VoxelIndex = voxelIndex
                };
            }

            _backer.AddMesh(_voxelGrid!.Info.Origin, _voxelGrid.Info.Size, _voxelGrid.Voxels.ToArray(), resInfo);

            _ray = _backer.CreateRayMarcher();

            bool res = _ray.Create(new VoxelLightRay
            {
                Position = new Vector3(0.02f, 1.9f, 0.02f),
                Direction = new Vector3(0, -1, 0),
                Energy = new Vector3(1, 1, 1)
            });

            var light = _backer.BakePointLight(new VoxPointLight
            {
                Color = new Vector3(1, 1, 1),
                Intensity = 10000,
                Position = new Vector3(0.02f, 1.9f, 0.02f),
                FalloffDistance = 7
            });

            var map = _backer.GetLightField();

        }

        [Action]
        public void Step()
        {
            if (_curVoxel.Parent == null)
                _host.Scene!.AddChild(_curVoxel);

            _ray.Step();

            var state = _ray.GetState();

            Log.Debug(this, "{0} / {1}: {2}", state.Cell, state.LastHitVoxel, state.LastVoxel.Status);

            if (state.LastHitVoxel != -1)
            {
                Console.WriteLine();
            }

            _curVoxel.WorldPosition = new Vector3(
                _voxelizer.GridDesc.Origin.X + (state.Cell.X + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Y + (state.Cell.Y + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Z + (state.Cell.Z + 0.5f) * _voxelizer.GridDesc.VoxelSize);

        }

    }
}

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace XrEngine.Lighting
{
    public class MeshVoxelizerManager : BaseComponent<TriangleMesh>
    {
        MeshVoxelizer _voxelizer;
        VoxelMeshView? _view;
        VoxelLightBaker _backer;
        private MeshVoxelGrid _voxelGrid;
        private VoxelRayMarcher _ray;

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
                var pos = _view.FaceInstances[i].Pos - _voxelGrid.Info.Origin;

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

            _backer.AddMesh(_voxelGrid.Info.Origin, _voxelGrid.Info.Size, _voxelGrid.Voxels.ToArray(), resInfo);

            _ray = _backer.CreateRayMarcher();

            bool res = _ray.Create(new VoxelLightRay
            {
                Position = new Vector3(0, 1.9f, 0),
                Direction = new Vector3(0, -1, 0),
                Energy = new Vector3(1, 1, 1)
            });

        }

        [Action]
        public void Step()
        {
            _ray.Step();

            var state = _ray.GetState();


        }

    }
}

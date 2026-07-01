using CanvasUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XrEngine.UI;
using XrMath;

namespace XrEngine.Lighting
{
    public class MeshVoxelizerManager : BaseComponent<TriangleMesh>
    {
        MeshVoxelizer _voxelizer;
        VoxelLightBaker _backer;

        TriangleMesh _meshView;
        CanvasView2D _canvas;
        MeshVoxelMaterial _meshMat;
        TriangleMesh _curVoxel;

        MeshVoxelGrid? _voxelGrid;
        VoxelRayMarcher? _ray;

        VoxelFaceInstance[]? _frontfaces;

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

            _meshMat = new MeshVoxelMaterial()
            {
                IsRemapMode = true
            };

            _meshView = new TriangleMesh(Quad3D.Default, _meshMat);

            _curVoxel = new TriangleMesh(Cube3D.Default, new ColorMaterial(Color.White));
            _curVoxel.Transform.SetScale(gridDesc.VoxelSize);

            _canvas = new CanvasView2D();
            _canvas.DrawCanvas += OnDraw;

            RayOrigin = new Vector3(0.02f, 1.9f, 0.02f);
            RayDir = new Vector3(0, -1, 0);
        }

        private void CompareSceneWithMesh(SceneVoxel[] scene)
        {
            if (_voxelGrid == null)
                return;

            var meshOrigin = _voxelGrid.Info.Origin;
            var meshSize = _voxelGrid.Info.Size;
            var grid = _voxelizer.GridDesc;

            var meshVoxels = _voxelGrid.Voxels.ToArray();

            var checkedCount = 0;
            var mismatchCount = 0;

            for (var z = 0; z < meshSize.Z; z++)
            {
                var gz = meshOrigin.Z + z;

                for (var y = 0; y < meshSize.Y; y++)
                {
                    var gy = meshOrigin.Y + y;

                    for (var x = 0; x < meshSize.X; x++)
                    {
                        var gx = meshOrigin.X + x;

                        var meshIndex =
                            x +
                            y * meshSize.X +
                            z * meshSize.X * meshSize.Y;

                        var sceneIndex =
                            gx +
                            gy * grid.Size.X +
                            gz * grid.Size.X * grid.Size.Y;

                        if ((uint)sceneIndex >= (uint)scene.Length)
                        {
                            Log.Warn(this, "Scene index out of range mesh=({0},{1},{2}) global=({3},{4},{5}) sceneIndex={6} sceneCount={7}",
                                x, y, z,
                                gx, gy, gz,
                                sceneIndex,
                                scene.Length);

                            mismatchCount++;
                            continue;
                        }

                        var meshVoxel = meshVoxels[meshIndex];
                        var sceneVoxel = scene[sceneIndex].Voxel;

                        checkedCount++;

                        if (!VoxelEquals(meshVoxel, sceneVoxel, out var reason))
                        {
                            Log.Warn(this, "Scene voxel mismatch mesh=({0},{1},{2}) global=({3},{4},{5}) meshIndex={6} sceneIndex={7}: {8}",
                                x, y, z,
                                gx, gy, gz,
                                meshIndex,
                                sceneIndex,
                                reason);

                            mismatchCount++;

                            if (mismatchCount >= 200)
                            {
                                Log.Warn(this, "Stopping comparison after {0} mismatches", mismatchCount);
                                return;
                            }
                        }
                    }
                }
            }

            Log.Info(this, "Scene/Mesh voxel compare done. Checked={0}, mismatches={1}", checkedCount, mismatchCount);
        }

        private static bool VoxelEquals(VoxelData a, VoxelData b, out string reason)
        {
            if (a.Status != b.Status)
            {
                reason = $"Status mesh={a.Status} scene={b.Status}";
                return false;
            }

            if (a.Occupancy != b.Occupancy)
            {
                reason = $"Occupancy mesh={a.Occupancy} scene={b.Occupancy}";
                return false;
            }

            for (var face = 0; face < VoxelLightConst.FaceCount; face++)
            {
                var fa = a.Faces[face];
                var fb = b.Faces[face];

                if (!FaceEquals(fa, fb, out var faceReason))
                {
                    reason = $"Face {face}: {faceReason}";
                    return false;
                }
            }

            reason = "";
            return true;
        }

        private static bool FaceEquals(VoxelFaceData a, VoxelFaceData b, out string reason)
        {
            if (a.Side != b.Side)
            {
                reason = $"Side mesh={a.Side} scene={b.Side}";
                return false;
            }

            if (a.TriangleId != b.TriangleId && a.TriangleId != -1 && b.TriangleId != 0)
            {
                reason = $"TriangleId mesh={a.TriangleId} scene={b.TriangleId}";
                return false;
            }

            if (a.UV != b.UV)
            {
                reason = $"UV mesh={a.UV} scene={b.UV}";
                return false;
            }

            if (a.HitPosition != b.HitPosition)
            {
                reason = $"HitPosition mesh={a.HitPosition} scene={b.HitPosition}";
                return false;
            }

            reason = "";
            return true;
        }

        public void Init()
        {
            Debug.Assert(_host?.Scene != null);

            if (_curVoxel.Parent == null)
                _host.Scene.AddChild(_curVoxel);

            if (_canvas.Parent == null)
                _host.Scene.AddChild(_canvas);

            if (_meshView.Parent == null)
                _host.Scene.AddChild(_meshView);
        }



        [Action]
        public void Apply()
        {
            Init();

            Log.Info(this, "Begin");

            _voxelGrid = _voxelizer.Voxelize(_host!.Geometry!, _host.WorldMatrix);

            _frontfaces = _voxelGrid.ExtractFrontFaces();

            _meshMat.Target = _host;
            _meshMat.GridDesc = _voxelizer.GridDesc;
            _meshMat.FaceInstances = _frontfaces;
            _meshMat.TargetIBuf = _host.IBuf;
            _meshMat.TargetVBuf = _host.VBuf;

            _meshView.InstanceCount = _meshMat.FaceInstances.Length;

            _meshMat.Invalidate();

            if (_meshView.Parent == null)
                _host!.Scene!.AddChild(_meshView);

            Log.Info(this, "Done");
        }

        private void OnDraw(ScreenCanvas obj)
        {
            obj.DrawCube(_curVoxel.WorldPosition, new Vector3(_voxelizer.GridDesc.VoxelSize),
                         "#ff0000", 3);

            obj.DrawLine(RayOrigin, RayOrigin + RayDir.Normalize() * 3f,
              "#ff0000", 3);
        }

        [Action]
        public void Backe()
        {
            if (_frontfaces == null)
                return;

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

            var resolved = _meshMat.ReadResolvedFaces();

            if (resolved == null)
                return;

            var resInfo = new VoxelMeshResolvedFace[resolved!.Length];

            for (var i = 0; i < _frontfaces!.Length; i++)
            {
                var pos = _frontfaces[i].Pos;

                var voxelIndex =
                    pos.X +
                    pos.Y * _voxelizer.GridDesc.Size.X +
                    pos.Z * _voxelizer.GridDesc.Size.X * _voxelizer.GridDesc.Size.Y;

                resInfo[i] = new VoxelMeshResolvedFace
                {
                    Resolved = resolved[i],
                    Data = _frontfaces[i].Data,
                    Face = _frontfaces[i].Face,
                    VoxelIndex = voxelIndex
                };
            }
            
            var ofs = new Vector3I(0, 0, 0);

            _backer.AddMesh(_voxelGrid!.Info.Origin, _voxelGrid.Info.Size, _voxelGrid.Voxels.ToArray(), resInfo);

            //var scene = _backer.GetScene().ToArray();
            //CompareSceneWithMesh(scene);

            _ray?.Dispose();
            _ray = _backer.CreateRayMarcher();

            _ray.Create(new VoxelLightRay
            {
                Position = RayOrigin,
                Direction = RayDir.Normalize(),
                Energy = new Vector3(1, 1, 1)
            });

            /*
            var lightMap = _backer.BakePointLight(new VoxPointLight
            {
                Color = new Vector3(1, 1, 1),
                Intensity = 10000,
                Position = new Vector3(0.02f, 1.9f, 0.02f),
                FalloffDistance = 7
            });

            var lightField = _backer.GetLightField();
            */
        }

        [Action]
        public void Step()
        {
            if (_ray == null)
                return;

            _ray.Step();

            var state = _ray.GetState();

            Log.Debug(this, "{0} / {1}: {2}", state.Cell, state.LastHitVoxel, state.LastVoxel.Status);

            _curVoxel.WorldPosition = new Vector3(
                _voxelizer.GridDesc.Origin.X + (state.Cell.X + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Y + (state.Cell.Y + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Z + (state.Cell.Z + 0.5f) * _voxelizer.GridDesc.VoxelSize);
        }


        public Vector3 RayOrigin { get; set; }

        public Vector3 RayDir { get; set; }
    }
}

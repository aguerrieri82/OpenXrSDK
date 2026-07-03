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
    public class LightFieldDebug : BaseComponent<TriangleMesh>, ILightFieldProvider
    {
        MeshVoxelizer _voxelizer;
        VoxelLightBaker _backer;

        TriangleMesh _meshView;
        TriangleMesh _fieldView;
        CanvasView2D _canvas;
        MeshVoxelMaterial _meshMat;
        LightFieldViewMaterial _fieldMat;
        TriangleMesh _curVoxel;
        FieldPhongMaterial _finalMat;

        MeshVoxelGrid? _meshGrid;
        VoxelRayMarcher? _ray;

        List<GpuVoxelFaceInstance> _faces = [];
        LightFieldData? _data;
        TriangleMesh[]? _walls;
        MeshVoxelGrid[] _wallGrid = new MeshVoxelGrid[6];

        public LightFieldDebug(VoxelGridDesc gridDesc)
        {
            _backer = new VoxelLightBaker();

            _finalMat = new FieldPhongMaterial();

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

            _fieldMat = new LightFieldViewMaterial();

            _fieldView = new TriangleMesh(new Cube3D(), _fieldMat);
            _fieldView.Flags |= EngineObjectFlags.NoFrustumCulling;
            _fieldView.Name = "Field View";
            _fieldView.IsVisible = false;

            _meshView = new TriangleMesh(new Quad3D(), _meshMat);
            _meshView.Flags |= EngineObjectFlags.NoFrustumCulling;
            _meshView.Name = "Mesh View";
            _meshView.IsVisible = false;

            _curVoxel = new TriangleMesh(Cube3D.Default, new ColorMaterial(Color.White));
            _curVoxel.Transform.SetScale(gridDesc.VoxelSize);
            _curVoxel.Name = "Voxel";

            _canvas = new CanvasView2D();
            _canvas.DrawCanvas += OnDraw;

            RayOrigin = new Vector3(0.02f, 1.9f, 0.02f);
            RayDir = new Vector3(0, -1, 0);
            RayEnergy = 5;

            LightRange = 7; 

            EnergyThreshold = 0.001f;
            MaxBounceCount = 5;
            RaySubsample = 6;
            SnapBounceDirection = false;
            InitiateLightField = false;
            ThreadCount = 10;

            MergeMode = VoxelLightMergeMode.MaxSample;

            BlurPasses = 3;
            BlurStrength = 1f;

            BucketSplitThreshold = 0.04f;

            EnableMultiBounceRays = false;
            BounceRayCount = 3;
            BounceRayDecay = 0.8f;
            BounceCenterWeight = 0.5f;
            BounceNormalWeight = 0.5f;
            BounceConeMaxAngle = 70.0f;

            CreateWalls();

            Context.Implement<ILightFieldProvider>(this);
        }

        private void CompareSceneWithMesh(VoxelData[] scene)
        {
            if (_meshGrid == null)
                return;

            var meshOrigin = _meshGrid.Info.Origin;
            var meshSize = _meshGrid.Info.Size;
            var grid = _voxelizer.GridDesc;

            var meshVoxels = _meshGrid.Voxels.ToArray();

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
                        var sceneVoxel = scene[sceneIndex];

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

        protected void CreateWalls()
        {
            var grid = _voxelizer.GridDesc;
            var cell = grid.VoxelSize;
            var size = grid.Size;
            var origin = grid.Origin;
            var padding = 1;

            var xMin = origin.X + cell;
            var xMax = origin.X + (size.X - padding) * cell;
            var yMin = origin.Y + cell;
            var yMax = origin.Y + (size.Y - padding) * cell;
            var zMin = origin.Z + cell;
            var zMax = origin.Z + (size.Z - padding) * cell;

            var cx = origin.X + size.X * cell * 0.5f;
            var cy = origin.Y + size.Y * cell * 0.5f;
            var cz = origin.Z + size.Z * cell * 0.5f;

            var sx = (size.X - padding * 2) * cell;
            var sy = (size.Y - padding * 2) * cell;
            var sz = (size.Z - padding * 2) * cell;

            var wallMaterial = new PbrV2Material
            {
                Color = Color.White,
                UseLightField = true,
            };

            _walls = new TriangleMesh[]
            {
                // Front, normal +Z
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix =
                        Matrix4x4.CreateScale(sx, sy, 1f) *
                        Matrix4x4.CreateTranslation(cx, cy, zMin)
                },

                // Back, normal -Z
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix =
                        Matrix4x4.CreateScale(sx, sy, 1f) *
                        Matrix4x4.CreateRotationY(MathF.PI) *
                        Matrix4x4.CreateTranslation(cx, cy, zMax)
                },

                // Left, normal +X
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix =
                        Matrix4x4.CreateScale(sz, sy, 1f) *
                        Matrix4x4.CreateRotationY(MathF.PI * 0.5f) *
                        Matrix4x4.CreateTranslation(xMin, cy, cz)
                },

                // Right, normal -X
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix =
                        Matrix4x4.CreateScale(sz, sy, 1f) *
                        Matrix4x4.CreateRotationY(-MathF.PI * 0.5f) *
                        Matrix4x4.CreateTranslation(xMax, cy, cz)
                },

                // Floor, normal +Y
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix =
                        Matrix4x4.CreateScale(sx, sz, 1f) *
                        Matrix4x4.CreateRotationX(-MathF.PI * 0.5f) *
                        Matrix4x4.CreateTranslation(cx, yMin, cz)
                },

                // Ceiling, normal -Y
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix =
                        Matrix4x4.CreateScale(sx, sz, 1f) *
                        Matrix4x4.CreateRotationX(MathF.PI * 0.5f) *
                        Matrix4x4.CreateTranslation(cx, yMax, cz)
                }
            };
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

            if (_fieldView.Parent == null)
                _host.Scene.AddChild(_fieldView);

            if (_host.Materials.Count == 1)
                _host.Materials.Add(_finalMat);
        }

        protected void ApplyWalls()
        {


            _meshMat.FaceInstances = _faces.ToArray();
            _meshMat.NotifyChanged(ChangeType.Render);
        }



        [Action]
        public void Apply()
        {
            Init();

            Log.Info(this, "Begin");

            _meshGrid = _voxelizer.Voxelize(_host!.Geometry!, _host.WorldMatrix);

            _faces.Clear();

            _faces.AddRange(_meshGrid.ExtractFaces(VoxelTriangleSide.All));

            int i = 0;
            foreach (var wall in _walls!)
            {
                if (wall.Parent == null)
                    _host!.Scene!.AddChild(wall);

                _wallGrid[i] = _voxelizer.Voxelize(wall.Geometry!, wall.WorldMatrix);

                var wallFaces = _wallGrid[i].ExtractFaces(VoxelTriangleSide.All);

                _faces.AddRange(wallFaces);

                i++;
            }


            _meshMat.Target = _host;
            _meshMat.GridDesc = _voxelizer.GridDesc;
            _meshMat.FaceInstances = _faces.ToArray();

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
            Init();

            _backer.ClearLightField();
            _backer.ClearScene();
            _backer.SetGrid(_voxelizer.GridDesc);

            _backer.SetParams(new VoxelLightBakeParams
            {
                EnergyThreshold = EnergyThreshold,
                MaxBounceCount = MaxBounceCount,
                ThreadCount = ThreadCount,
                RaySubsample = RaySubsample,
                SnapBounceDirection = SnapBounceDirection,
                InitiateLightField = InitiateLightField,
                BlurStrength = BlurStrength,
                BlurPasses = BlurPasses,

                FillEmptyDir = true,
                MergeMode = MergeMode,
                NormalizeDir = false,

                BucketSplitThreshold = BucketSplitThreshold,

                EnableMultiBounceRays = EnableMultiBounceRays,
                BounceRayCount = BounceRayCount,
                BounceRayDecay = BounceRayDecay,
                BounceCenterWeight = BounceCenterWeight,
                BounceNormalWeight = BounceNormalWeight,
                BounceConeMaxAngle = BounceConeMaxAngle,
            });

            if (_faces.Count > 0)
            {
                var resolved = _meshMat.ReadResolvedFaces();

                if (resolved == null)
                    return;

                var resInfo = new VoxelMeshResolvedFace[resolved!.Length];

                for (var j = 0; j < _faces.Count; j++)
                {
                    resInfo[j] = new VoxelMeshResolvedFace
                    {
                        BaseColor = resolved[j].BaseColor,
                        Normal = resolved[j].Normal,
                        Metallic = resolved[j].Metallic,
                        Roughness = resolved[j].Roughness,
                        Face = _faces[j].Face,
                        VoxelIndex = _backer.CellIndex(_faces[j].Pos)
                    };
                }

                var ofs = new Vector3I(0, 0, 0);

                _backer.AddMesh(_meshGrid!.Info.Origin, _meshGrid.Info.Size, _meshGrid.Voxels.ToArray(), resInfo);

                int i = 0;

                foreach (var wall in _walls!)
                {
                    var wallFaces = _wallGrid[i].ExtractFaces(VoxelTriangleSide.All);

                    var wallResolved = new VoxelMeshResolvedFace[wallFaces.Length];

                    for (var j = 0; j < wallFaces.Length; j++)
                    {
                        wallResolved[j] = new VoxelMeshResolvedFace
                        {
                            BaseColor = new Vector4(1, 1, 1, 1),
                            Normal = Vector3.TransformNormal( wall.Geometry!.Vertices[0].Normal, wall.WorldMatrix).Normalize(),
                            Roughness = 0.2f,
                            Metallic = 0.0f,
                            Face = wallFaces[j].Face,
                            VoxelIndex = _backer.CellIndex(wallFaces[j].Pos)
                        };
                    }

                    _backer.AddMesh(
                        _wallGrid[i].Info.Origin,
                        _wallGrid[i].Info.Size,
                        _wallGrid[i].Voxels.ToArray(),
                        wallResolved);

                    i++;
                }
            }

            _ray?.Dispose();
            _ray = _backer.CreateRayMarcher();

            _ray.Create(new VoxelLightRay
            {
                Position = RayOrigin,
                Direction = RayDir.Normalize(),
                Energy = Vector3.One * RayEnergy
            });

            Log.Info(this, "Backing point light");

            var lightMap = _backer.BakePointLight(new VoxPointLight
            {
                Color = new Vector3(1, 1, 1),
                Intensity = RayEnergy,
                Position = RayOrigin,
                Falloff = new LightFalloff
                {
                    Type = LightFalloffType.Linear,
                    Factor = 1f,
                    Range = LightRange
                }
            });

            Log.Info(this, "Accumulate");

            _backer.AccumulateLight(lightMap);

            Log.Info(this, "Extarct light field");

            var lightField = _backer.GetLightField();

            _fieldView.InstanceCount = lightField.Size.Area();

            _fieldMat.VoxelSize = _backer.GridDesc.VoxelSize;
            _fieldMat.Origin = _backer.GridDesc.Origin;
            _fieldMat.Size = _backer.GridDesc.Size;
            _fieldMat.Textures = _backer.CreateTextures();
            _fieldMat.Invalidate();


            _finalMat.IsEnabled = false;
            _finalMat.VoxelSize = _backer.GridDesc.VoxelSize;
            _finalMat.Origin = _backer.GridDesc.Origin;
            _finalMat.Size = _backer.GridDesc.Size;
            _finalMat.Textures = _backer.CreateTextures();
            _finalMat.Invalidate();

            PbrV2Material.SHADER.UseLightField = true;
            PbrV2Material.SHADER.NotifyChanged(ChangeType.Render);

            ((PbrV2Material)_host!.Materials[0]).UseLightField = true;
            ((PbrV2Material)_host!.Materials[0]).NotifyChanged(ChangeType.Render);

            foreach (var light in _host.Scene!.Descendants<Light>())
                light.IsVisible = false;

            Log.Info(this, "Texture loaded");
        }

        [Action]
        public void Step()
        {
            if (_ray == null)
                return;

            _ray.Step();

            var state = _ray.GetState();

            Log.Debug(this, "{0} / {1}: {2}", state.Cell, state.LastAffectedVoxel, state.LastVoxel.Status);

            _curVoxel.WorldPosition = new Vector3(
                _voxelizer.GridDesc.Origin.X + (state.Cell.X + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Y + (state.Cell.Y + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Z + (state.Cell.Z + 0.5f) * _voxelizer.GridDesc.VoxelSize);
        }

        public LightFieldData GetLightField()
        {
            _data ??= new LightFieldData();

            _data.Textures = _fieldMat.Textures;
            _data.Strength = 1;
            _data.Origin = _backer.GridDesc.Origin;
            _data.Size = _backer.GridDesc.Size;
            _data.VoxelSize = _backer.GridDesc.VoxelSize;

            return _data;
        }

        public Vector3 RayOrigin { get; set; }

        public Vector3 RayDir { get; set; }

        public float RayEnergy { get; set; }

        public float EnergyThreshold { get; set; }

        public int MaxBounceCount { get; set; }

        public int ThreadCount { get; set; }

        public int RaySubsample { get; set; }

        public bool SnapBounceDirection { get; set; }

        public bool InitiateLightField { get; set; }

        public float LightRange { get; set; }
        
        public float BlurStrength { get; set; }

        public int BlurPasses { get; set; }

        public float BucketSplitThreshold { get; set; }

        public VoxelLightMergeMode MergeMode { get; set; }

        public bool EnableMultiBounceRays { get; set; }

        public int BounceRayCount { get; set; }
        public float BounceRayDecay { get; set; }
        public float BounceCenterWeight { get; set; }
        public float BounceNormalWeight { get; set; }
        public float BounceConeMaxAngle { get; set; }
    }
}

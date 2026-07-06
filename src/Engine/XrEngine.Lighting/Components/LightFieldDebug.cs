using CanvasUI;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XrEngine.OpenGL;
using XrEngine.UI;
using XrMath;

namespace XrEngine.Lighting
{
    [StateManager(StateManagerMode.Auto)]
    public class LightFieldDebug : BaseComponent<TriangleMesh>, ILightFieldProvider, IDrawGizmos
    {
        MeshVoxelizer _voxelizer;
        GpuSceneVoxelizer _gpuVoxelizer;
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

        LightFieldData? _data;
        TriangleMesh[]? _walls;
        VoxelGridDesc _gridDesc;
        private List<GpuVoxelFaceData> _faces;
        private VoxelLightFieldView _field;

        public LightFieldDebug(VoxelGridDesc gridDesc)
        {
            _gridDesc = gridDesc;   

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
                IsRemapMode = false
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

            RayMergeMode = VoxelLightMergeMode.MaxSample;
            LightMergeMode = VoxelLightMergeMode.Add;
            GenMergeMode = VoxelLightMergeMode.AddPreserveDir;

            BlurPasses = 3;
            BlurStrength = 1f;

            BounceRayCount = 3;
            BounceRayDecay = 0.8f;
            BounceCenterWeight = 0.5f;
            BounceNormalWeight = 0.5f;
            BounceConeMaxAngle = MathF.PI * (70f / 180f);

            SmoothDirIterations = 32;
            SmoothDirMaxSlope = 1f;
            SmoothDirRelaxation = 0.75f;
            SmoothDirSmoothness = 0.05f;

            LightFallOff = LightFalloffType.Quadratic;

            Strength = 1;

            CreateWalls();

            Context.Implement<ILightFieldProvider>(this);
        }


        public override void GetState(IStateContainer container)
        {
            container.WriteObject(this, GetType());
            base.GetState(container);
        }


        protected override void SetStateWork(IStateContainer container)
        {
            container.ReadObject(this, GetType());
            base.SetStateWork(container);
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
                Metalness= 0,
                Roughness = 0.8f
            };

            _walls =
            [
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
            ];
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

            foreach (var wall in _walls!)
            {
                if (wall.Parent == null)
                    _host.Scene.AddChild(wall);
            }
     
        }



        [Action]
        public void Apply()
        {
            Init();

            Log.Info(this, "Begin");

            _gpuVoxelizer = new GpuSceneVoxelizer(OpenGLRender.Current!.GL, 32);

            XrEngine.EngineNativeLib.RdcStartFrameCapture();
            
            Log.Info(this, "Begin voxelize");

            _faces = _gpuVoxelizer.Voxelize([_host!, .._walls!], _gridDesc);

            Log.Info(this, "End voxelize");

            XrEngine.EngineNativeLib.RdcEndFrameCapture(true);

            _meshMat.Target = _host;
            _meshMat.GridDesc = _voxelizer.GridDesc;
            _meshMat.FaceInstances = _faces.Select(a => new GpuVoxelFaceInstance
            {
                Face = a.Face,
                Pos = a.Cell,
                TriangleId = 1,
            }).ToArray();

            _meshView.InstanceCount = _meshMat.FaceInstances.Length;

            _meshMat.Invalidate();

            if (_meshView.Parent == null)
                _host!.Scene!.AddChild(_meshView);

            Log.Info(this, "Done");
        }

        public void DrawGizmos(Canvas3D canvas)
        {

            canvas.State.Color = "#ff0000";

            canvas.DrawLine(RayOrigin, RayOrigin + RayDir.Normalize() * 3f);
        }

        private void OnDraw(ScreenCanvas obj)
        {
            obj.DrawCube(_curVoxel.WorldPosition, new Vector3(_voxelizer.GridDesc.VoxelSize),
                         "#ff0000", 3);
            /*
            obj.DrawLine(RayOrigin, RayOrigin + RayDir.Normalize() * 3f,
              "#ff0000", 3);*/
        }

        protected void UpdateParams()
        {

            _backer.SetParams(new VoxelLightBakeParams
            {
                EnergyThreshold = EnergyThreshold,
                ThreadCount = ThreadCount,
                RaySubsample = RaySubsample,
                SnapBounceDirection = SnapBounceDirection,
                InitiateLightField = InitiateLightField,
                BlurStrength = BlurStrength,
                BlurPasses = BlurPasses,

                FillEmptyDir = FillEmptyDir,
                RayMergeMode = RayMergeMode,
                GenMergeMode = GenMergeMode,
                LightMergeMode = LightMergeMode,
                NormalizeDir = false,
                DirCollapseMode = DirCollapseMode,

                Bounce = new BounceParams
                {
                    MaxCount = MaxBounceCount,
                    RayCount = BounceRayCount,
                    RayDecay = BounceRayDecay,
                    CenterWeight = BounceCenterWeight,
                    NormalWeight = BounceNormalWeight,
                    ConeMaxAngle = BounceConeMaxAngle
                },

                SmoothDir = new SmoothDirParams
                {
                    Iterations = SmoothDirIterations,
                    MaxSlope = SmoothDirMaxSlope,
                    Relaxation = SmoothDirRelaxation,
                    Smoothness = SmoothDirSmoothness
                }
            });
        }

        [Action]
        public void Backe()
        {
            if (_faces == null)
                return;

            Init();

            _backer.ClearLightField();
            _backer.ClearScene();
            _backer.SetGrid(_voxelizer.GridDesc);

            UpdateParams();

            if (_faces.Count > 0)
                _backer.AddMesh(_faces.ToArray());

            _ray?.Dispose();
            _ray = _backer.CreateRayMarcher();

            _ray.Create(new VoxelLightRay
            {
                Position = RayOrigin,
                Direction = RayDir.Normalize(),
                Energy = Vector3.One * RayEnergy,
                OriginTotalDistance = 0,
                Falloff = new LightFalloff
                {
                    Type = LightFallOff,
                    Factor = 1f,
                    Range = LightRange
                }
            });

            Log.Info(this, "Backing point light");

            var lightMap = _backer.BakePointLight(new VoxPointLight
            {
                Color = new Vector3(1, 1, 1),
                Intensity = RayEnergy,
                Position = RayOrigin,
                Falloff = new LightFalloff
                {
                    Type = LightFallOff,
                    Factor = 1f,
                    Range = LightRange
                }
            });

            Log.Info(this, "Accumulate");

            _backer.AccumulateLight(lightMap);

            Log.Debug(this, "Accumulate end");


            Extract();
        }

        unsafe void AdjustField()
        {

            var count2 = 0;
            var count3 = 0;
            for (var i = 0; i < _field.CellCount; i++)
            {
                var count = 0;
                var sum = Vector3.Zero;
                for (var j = 0; j < 6; j++)
                {
                    var span = new Span<Vector3>((Vector*)_field.Color[j], _field.CellCount);
                    var color = span[i];
                    if (color.Length() > 0)
                        count++;
                    sum += color;
                }

                if (count == 2)
                    count2++;
                
                if (count == 3)
                    count3++;

                if (count > 1)
                {
                    var energy = sum / (float)(count * count);

                    for (var j = 0; j < 6; j++)
                    {
                        var span = new Span<Vector3>((Vector*)_field.Color[j], _field.CellCount);
                        var color = span[i];

                        if (color.Length() > 0)
                            span[i] = energy;
                    }
                }
            }

            Log.Warn(this, "2: {0} - 3:{1} - T: {2}", count2, count3, _field.CellCount);
        }



        [Action]
        public void Extract()
        {
            Log.Info(this, "Extract light field");

            UpdateParams();

            _field = _backer.GetLightField(true);

            //AdjustField();

            _fieldView.InstanceCount = _field.Size.Area();

            if (_fieldMat.Textures != null)
            {
                foreach (var tex in _fieldMat.Textures)
                    tex.Dispose();
            }

            var textures = _backer.CreateTextures();

            _fieldMat.VoxelSize = _backer.GridDesc.VoxelSize;
            _fieldMat.Origin = _backer.GridDesc.Origin;
            _fieldMat.Size = _backer.GridDesc.Size;
            _fieldMat.Textures = textures;
            _fieldMat.Invalidate();

            _finalMat.IsEnabled = false;
            _finalMat.VoxelSize = _backer.GridDesc.VoxelSize;
            _finalMat.Origin = _backer.GridDesc.Origin;
            _finalMat.Size = _backer.GridDesc.Size;
            _finalMat.Textures = textures;
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

            var res = _ray.Step();

            var state = _ray.GetState();

            int fillFaces = 0;

            foreach (var face in state.LastVoxel.Faces)
            {
                if (face.TriangleId > 0)
                    fillFaces++;
            }

            Log.Debug(this, "{4}{0} / {1}  E: {2} - F: {3}", state.Cell, state.LastAffectedVoxel,  state.Energy.Length(), fillFaces, !res ? "[DEAD] " : "");

            _curVoxel.WorldPosition = new Vector3(
                _voxelizer.GridDesc.Origin.X + (state.Cell.X + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Y + (state.Cell.Y + 0.5f) * _voxelizer.GridDesc.VoxelSize,
                _voxelizer.GridDesc.Origin.Z + (state.Cell.Z + 0.5f) * _voxelizer.GridDesc.VoxelSize);
        }

        public LightFieldData GetLightField()
        {
            _data ??= new LightFieldData();

            _data.Textures = _fieldMat.Textures;
            _data.Strength = Strength;
            _data.Origin = _backer.GridDesc.Origin;
            _data.Size = _backer.GridDesc.Size;
            _data.VoxelSize = _backer.GridDesc.VoxelSize;

            return _data;
        }



        [Category("Trace")]
        public Vector3 RayOrigin { get; set; }

        [Category("Trace")]
        public Vector3 RayDir { get; set; }

        [Category("Trace")]
        public float RayEnergy { get; set; }

        [Category("Trace")]
        public float EnergyThreshold { get; set; }

        [Category("Trace")]
        public int RaySubsample { get; set; }

        [Category("Trace")]
        public float LightRange { get; set; }

        [Category("Trace")]
        public LightFalloffType LightFallOff { get; set; }

        [Category("Trace")]
        public VoxelLightMergeMode RayMergeMode { get; set; }

        [Category("Trace")]
        public VoxelLightMergeMode GenMergeMode { get; set; }

        [Category("Trace")]
        public VoxelLightMergeMode LightMergeMode { get; set; }


        [Category("Misc")]
        public int ThreadCount { get; set; }

        [Category("Misc")]
        public bool SnapBounceDirection { get; set; }

        [Category("Misc")]
        public bool InitiateLightField { get; set; }

        [Category("Misc")]
        [Range(0,1, 0.01f)]
        public float Strength { get; set; }


        [Category("Blur")]
        public float BlurStrength { get; set; }

        [Category("Blur")]
        public int BlurPasses { get; set; }


        [Category("Bounce")]
        public int MaxBounceCount { get; set; }

        [Category("Bounce")]
        public int BounceRayCount { get; set; }

        [Category("Bounce")]
        public float BounceRayDecay { get; set; }

        [Category("Bounce")]
        public float BounceCenterWeight { get; set; }

        [Category("Bounce")]
        public float BounceNormalWeight { get; set; }

        [Category("Bounce")]
        [ValueType(ValueType.Radiant)]
        public float BounceConeMaxAngle { get; set; }

        [Category("Field Dir")]
        public bool FillEmptyDir { get; set; }

        [Category("Field Dir")]
        public DirectionCollapseMode DirCollapseMode { get; set; }    

        [Category("Field Dir")]
        public int SmoothDirIterations { get; set; }

        [Category("Field Dir")]
        public float SmoothDirMaxSlope { get; set; }

        [Category("Field Dir")]
        public float SmoothDirRelaxation { get; set; }

        [Category("Field Dir")]
        public float SmoothDirSmoothness { get; set; }
    }
}

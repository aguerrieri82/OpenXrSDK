using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.Lighting
{
    [StateManager(StateManagerMode.Auto)]
    public class LightFieldDebugV2 : BaseComponent<Scene3D>, IDrawGizmos, ILightFieldProvider
    {
        readonly TriangleMesh _meshView;
        readonly MeshVoxelMaterial _meshMat;
        readonly TriangleMesh _curVoxel;
        readonly VoxelLightBakerV2 _baker;
        readonly GpuMeshVoxelizer _gpuVoxelizer;
        readonly LightFieldDataV2 _fieldData;
        readonly HashSet<LightFieldEmitter> _activeEmitters = [];
        readonly HashSet<LightFieldReceiver> _activeOccluders = [];

        TriangleMesh[]? _walls;
        VoxelGridDesc _grid;
        VoxelLightBakeParamsV2 _lastParams;
        VoxelLightBakeParams _lastBakeParams;
        bool _hasParams;

        public LightFieldDebugV2(VoxelGridDesc grid)
        {
            _grid = grid;

            _baker = new VoxelLightBakerV2();
            _baker.SetGrid(grid);

            _gpuVoxelizer = new GpuMeshVoxelizer(OpenGLRender.Current!.GL);
            _gpuVoxelizer.SetGrid(grid);

            _fieldData = new LightFieldDataV2
            {
                Origin = grid.Origin,
                Size = grid.Size,
                VoxelSize = grid.VoxelSize
            };

            _meshMat = new MeshVoxelMaterial();

            _meshView = new TriangleMesh(new Quad3D(), _meshMat);
            _meshView.Flags |= EngineObjectFlags.NoFrustumCulling;
            _meshView.Name = "Mesh View";
            _meshView.IsVisible = false;

            _curVoxel = new TriangleMesh(Cube3D.Default, new ColorMaterial(Color.White));
            _curVoxel.Transform.SetScale(_grid.VoxelSize);
            _curVoxel.Name = "Voxel";

            TrackMode = LightTrackMode.Full;
            EnergyThreshold = 0.001f;
            MaxBounceCount = 5;
            RaySubsample = 6;
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

            RecoveryRange = 2;

            AngularTolerance = MathF.PI * (10f / 180f);
            RelativeEnergyTolerance = 0.01f;

            CreateWalls();

            Context.Implement<ILightFieldProvider>(this);

            PbrMaterial.SHADER.UseLightField = true;
        }

        protected override void OnAttach()
        {
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

        void CreateWalls()
        {
            var cell = _grid.VoxelSize;
            var size = _grid.Size;
            var origin = _grid.Origin;
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

            var wallMaterial = new PbrMaterial
            {
                Color = Color.White,
                UseLightField = UseLightFieldMode.Self,
                Metalness = 0,
                Roughness = 0.8f
            };

            _walls =
            [
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix = Matrix4x4.CreateScale(sx, sy, 1f) * Matrix4x4.CreateTranslation(cx, cy, zMin)
                },
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix = Matrix4x4.CreateScale(sx, sy, 1f) * Matrix4x4.CreateRotationY(MathF.PI) * Matrix4x4.CreateTranslation(cx, cy, zMax)
                },
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix = Matrix4x4.CreateScale(sz, sy, 1f) * Matrix4x4.CreateRotationY(MathF.PI * 0.5f) * Matrix4x4.CreateTranslation(xMin, cy, cz)
                },
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix = Matrix4x4.CreateScale(sz, sy, 1f) * Matrix4x4.CreateRotationY(-MathF.PI * 0.5f) * Matrix4x4.CreateTranslation(xMax, cy, cz)
                },
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix = Matrix4x4.CreateScale(sx, sz, 1f) * Matrix4x4.CreateRotationX(-MathF.PI * 0.5f) * Matrix4x4.CreateTranslation(cx, yMin, cz)
                },
                new TriangleMesh(new Quad3D(), wallMaterial)
                {
                    WorldMatrix = Matrix4x4.CreateScale(sx, sz, 1f) * Matrix4x4.CreateRotationX(MathF.PI * 0.5f) * Matrix4x4.CreateTranslation(cx, yMax, cz)
                }
            ];

            foreach (var wall in _walls)
                wall.AddComponent<LightFieldReceiver>();
        }

        public void Init()
        {
            Debug.Assert(_host?.Scene != null);

            if (_curVoxel.Parent == null)
                _host.Scene.AddChild(_curVoxel);

            if (_meshView.Parent == null)
                _host.Scene.AddChild(_meshView);

            foreach (var wall in _walls!)
            {
                if (wall.Parent == null)
                    _host.Scene.AddChild(wall);
            }
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
        }

        (bool BakeDirty, bool BuildDirty) UpdateParams()
        {
            var parameters = new VoxelLightBakeParamsV2
            {
                Base = new VoxelLightBakeParams
                {
                    Mode = TrackMode,
                    EnergyThreshold = EnergyThreshold,
                    ThreadCount = ThreadCount,
                    RaySubsample = RaySubsample,
                    InitiateLightField = InitiateLightField,
                    RayMergeMode = RayMergeMode,
                    GenMergeMode = GenMergeMode,
                    LightMergeMode = LightMergeMode,
                    DirCollapseMode = DirCollapseMode,
                    IntersectMode = IntersectionMode,
                    NormalizeDir = false,
                    Blur = new BlurParams
                    {
                        Strength = BlurStrength,
                        Passes = BlurPasses,
                        ColorOnly = BlurColorOnly
                    },
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
                    },
                    Recovery = new LightCurve
                    {
                        Factor = 1,
                        Type = LightCurveType.Quadratic,
                        Range = RecoveryRange
                    }
                },
                AngularTolerance = AngularTolerance,
                RelativeEnergyTolerance = RelativeEnergyTolerance
            };

            var bakeParams = parameters.Base;
            bakeParams.Blur = default;

            var bakeDirty = !_hasParams || !bakeParams.Equals(_lastBakeParams);
            var buildDirty = !_hasParams || !parameters.Equals(_lastParams);

            _baker.SetParams(parameters);

            _lastParams = parameters;
            _lastBakeParams = bakeParams;
            _hasParams = true;

            return (bakeDirty, buildDirty);
        }

        bool UpdateScene()
        {
            var meshDirty = false;

            foreach (var mesh in _host.Descendants<TriangleMesh>())
            {
                if (!mesh.TryComponent<LightFieldReceiver>(out var rec))
                    continue;

                if (!rec.IsEnabled || !rec.IsOccluder)
                {
                    if (_activeOccluders.Remove(rec))
                        meshDirty = true;

                    continue;
                }

                if (_activeOccluders.Add(rec))
                    meshDirty = true;

                if (rec.NeedUpdate)
                    meshDirty = true;
            }

            if (!meshDirty)
                return false;

            _baker.ClearScene();

            foreach (var mesh in _host.Descendants<TriangleMesh>())
            {
                if (!mesh.TryComponent<LightFieldReceiver>(out var rec) || !rec.IsEnabled || !rec.IsOccluder)
                    continue;

                if (rec.NeedUpdate || rec.Voxels == null)
                    rec.UpdateVoxels(_gpuVoxelizer);

                Debug.Assert(rec.Voxels != null);
                _baker.AddMesh(rec.Voxels);
            }

            return true;
        }



        bool BakeLights(bool force)
        {
            var lightDirty = force;

            foreach (var light in _host.Descendants<Light>())
            {
                if (!light.TryComponent<LightFieldEmitter>(out var emitter))
                    continue;

                if (force)
                    emitter.InvalidateV2();

                if (!emitter.IsEnabled)
                {
                    if (_activeEmitters.Remove(emitter))
                        lightDirty = true;

                    continue;
                }

                if (_activeEmitters.Add(emitter))
                    lightDirty = true;

                if (emitter.NeedUpdateV2 || emitter.ContributionsV2 == null)
                    lightDirty = true;
            }

            if (!lightDirty)
                return false;

            _baker.ClearLightField();

            foreach (var light in _host.Descendants<Light>())
            {
                light.IsVisible = false;

                if (!light.TryComponent<LightFieldEmitter>(out var emitter) || !emitter.IsEnabled)
                    continue;

                if (emitter.NeedUpdateV2 || emitter.ContributionsV2 == null)
                    emitter.UpdateLight(_baker);

                if (emitter.ContributionsV2 != null)
                {
                    Log.Info(this, "Accumulate {0}", light.Name ?? light.GetType().Name);
                    _baker.AccumulateLight(emitter.ContributionsV2);
                }
            }

            return true;
        }

        void BuildField()
        {
            var field = _baker.BuildLightField(AngularTolerance, RelativeEnergyTolerance);
            var source = _baker.GetContributions();

            var lobes = MemoryMarshal.Cast<VoxelLightGpuContribution, LightFieldLobe>(source);

            var lookup = _baker.CreateLookupTexture(TextureFormat.RgUInt32);

            _fieldData.LookupTexture?.Dispose();
            _fieldData.LookupTexture = lookup;
            _fieldData.Contributions = lobes.ToArray();
            _fieldData.Origin = _grid.Origin;
            _fieldData.Size = field.Size;
            _fieldData.VoxelSize = _grid.VoxelSize;
            _fieldData.Version++;

            Log.Info(this, "Light field V2 built: {0} lobes", lobes.Length);
        }

        void UpdateMeshView()
        {
            var faces = new List<GpuVoxelFaceInstance>();

            foreach (var mesh in _host.Descendants<TriangleMesh>())
            {
                if (mesh.TryComponent<LightFieldReceiver>(out var rec) && rec.IsEnabled && rec.IsOccluder && rec.Voxels != null)
                {
                    faces.AddRange(rec.Voxels.Select(a => new GpuVoxelFaceInstance
                    {
                        Face = a.Face,
                        Pos = a.Cell,
                        BaseColor = a.BaseColor,
                        Metallic = a.Metallic,
                        Normal = a.Normal,
                        Roughness = a.Roughness
                    }));
                }
            }

            _meshMat.GridDesc = _grid;
            _meshMat.LoadFaces(faces.ToArray());
            _meshView.InstanceCount = faces.Count;
            _meshMat.Invalidate();
        }

        [Action]
        public void Backe()
        {
            Init();

            var paramState = UpdateParams();
            var meshDirty = UpdateScene();

            if (meshDirty)
                UpdateMeshView();

            var lightDirty = BakeLights(meshDirty || paramState.BakeDirty);

            if (lightDirty || paramState.BuildDirty)
            {
                BuildField();
                PbrMaterial.SHADER.NotifyChanged(ChangeType.Render);
            }
        }

        [Action]
        public void Extract()
        {
            var paramState = UpdateParams();

            if (paramState.BakeDirty)
                BakeLights(true);

            BuildField();
            PbrMaterial.SHADER.NotifyChanged(ChangeType.Render);
        }

        [Action]
        public void CopyPreset()
        {
            UpdateParams();

            var clip = Context.Require<IClipboard>();
            var json = JsonSerializer.Serialize(_baker.Params, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                Converters = { new JsonStringEnumConverter() }
            });

            clip.Copy(json, "application/json");
        }

        public void LoadSettings(string name)
        {
            var path = Path.Combine(StorePath!, "LightField", name + ".json");

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var state = new JsonStateContainer(json);
                SetState(state);
            }
            else
                Log.Warn(this, "Settings '{0}' not found", name);
        }

        public LightFieldData GetLightField() => throw new NotSupportedException("LightFieldDebugV2 provides only light field V2.");

        public LightFieldDataV2 GetLightFieldV2() => _fieldData;

        public LightFieldVersion Versions => LightFieldVersion.V2;

        [Category("Trace")]
        public LightTrackMode TrackMode { get; set; }

        [Category("Trace")]
        public float EnergyThreshold { get; set; }

        [Category("Trace")]
        public int RaySubsample { get; set; }

        [Category("Trace")]
        public VoxelLightMergeMode RayMergeMode { get; set; }

        [Category("Trace")]
        public VoxelLightMergeMode GenMergeMode { get; set; }

        [Category("Trace")]
        public VoxelLightMergeMode LightMergeMode { get; set; }

        [Category("Trace")]
        public float RecoveryRange { get; set; }

        [Category("Trace")]
        public RayIntersectionMode IntersectionMode { get; set; }

        [Category("Misc")]
        public int ThreadCount { get; set; }

        [Category("Misc")]
        public bool InitiateLightField { get; set; }

        public string? StorePath { get; set; }

        [Category("Blur")]
        public float BlurStrength { get; set; }

        [Category("Blur")]
        public int BlurPasses { get; set; }

        [Category("Blur")]
        public bool BlurColorOnly { get; set; }

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
        public DirectionCollapseMode DirCollapseMode { get; set; }

        [Category("Field Dir")]
        public int SmoothDirIterations { get; set; }

        [Category("Field Dir")]
        public float SmoothDirMaxSlope { get; set; }

        [Category("Field Dir")]
        public float SmoothDirRelaxation { get; set; }

        [Category("Field Dir")]
        public float SmoothDirSmoothness { get; set; }

        [Category("Field V2")]
        [ValueType(ValueType.Radiant)]
        public float AngularTolerance { get; set; }

        [Category("Field V2")]
        [Range(0, 1, 0.001f)]
        public float RelativeEnergyTolerance { get; set; }

        [Category("Field V2")]
        [Range(0, 1, 0.01f)]
        public float DiffuseStrength
        {
            get => _fieldData.DiffuseStrength;
            set => _fieldData.DiffuseStrength = value;
        }

        [Category("Field V2")]
        [Range(0, 1, 0.01f)]
        public float SpecularStrength
        {
            get => _fieldData.SpecularStrength;
            set => _fieldData.SpecularStrength = value;
        }

        public VoxelLightBakerV2 Baker => _baker;
    }
}

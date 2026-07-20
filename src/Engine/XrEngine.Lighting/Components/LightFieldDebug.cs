using Common.Interop;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine.UI;
using XrMath;

namespace XrEngine.Lighting
{
    [StateManager(StateManagerMode.Auto)]
    public class LightFieldDebug : BaseComponent<Scene3D>, IDrawGizmos
    {
        readonly TriangleMesh _meshView;
        readonly TriangleMesh _fieldView;
        readonly CanvasView2D _canvas;
        readonly MeshVoxelMaterial _meshMat;
        readonly LightFieldViewMaterial _fieldMat;
        readonly TriangleMesh _curVoxel;
        readonly bool _isReadMode;

        VoxelRayMarcher? _ray;
        TriangleMesh[]? _walls;
        VoxelGridDesc _grid;
        LightFieldProvider? _provider;

        public LightFieldDebug(VoxelGridDesc grid, bool isReadMode)
        {
            _grid = grid;

            _isReadMode = isReadMode;

            _meshMat = new MeshVoxelMaterial();

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
            _curVoxel.Transform.SetScale(_grid.VoxelSize);
            _curVoxel.Name = "Voxel";

            _canvas = new CanvasView2D();
            _canvas.DrawCanvas += OnDraw;

            TrackMode = LightTrackMode.Full;

            RayOrigin = new Vector3(0.02f, 1.9f, 0.02f);
            RayDir = new Vector3(0, -1, 0);
            RayEnergy = 5;

            LightRange = 7;

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

            LightFallOff = LightCurveType.Quadratic;

            RecoveryRange = 2;

            CreateWalls();
        }

        protected override void OnAttach()
        {
            _provider = _host!.Component<LightFieldProvider>();
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

        protected void CreateWalls()
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

            foreach (var wall in _walls!)
                wall.AddComponent<LightFieldReceiver>();
        }

        public void Init()
        {
            Debug.Assert(_host?.Scene != null);

            if (_curVoxel.Parent == null)
                _host.Scene.AddChild(_curVoxel);

            if (_canvas.Parent == null && !_isReadMode)
                _host.Scene.AddChild(_canvas);

            if (_meshView.Parent == null)
                _host.Scene.AddChild(_meshView);

            if (_fieldView.Parent == null)
                _host.Scene.AddChild(_fieldView);

            foreach (var wall in _walls!)
            {
                if (wall.Parent == null)
                    _host.Scene.AddChild(wall);
            }
        }

        public void DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            canvas.State.Color = "#ff0000";

            canvas.DrawLine(RayOrigin, RayOrigin + RayDir.Normalize() * 3f);
        }

        private void OnDraw(ScreenCanvas obj)
        {
            obj.DrawCube(_curVoxel.WorldPosition, new Vector3(_grid.VoxelSize),
                         "#ff0000", 3);
        }

        protected void UpdateParams()
        {
            _provider!.LoadProfile(new VoxelLightBakeParams
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
            });

        }

        public unsafe static IMemoryBuffer<byte> ExtractFaceDirection2Texture(
            IMemoryBuffer<byte> sourceData,
            int width,
            int height,
            int depth,
            int face,
            int sourceChannels)
        {
            var target = MemoryBuffer.Create<byte>((uint)(width * height * depth * 2 * sizeof(float)));

            using var srcLock = sourceData.MemoryLock();

            using var dstLock = target.MemoryLock();

            var source = (float*)srcLock.Data;
            var dest = (float*)dstLock.Data;

            var axis = face < 2 ? 0 : face < 4 ? 1 : 2;
            var a = face < 2 ? 1 : 0;
            var b = face < 4 ? 2 : 1;

            for (var i = 0; i < width * height * depth; i++)
            {
                var s = i * sourceChannels;
                var t = i * 2;

                var inv = 1f / MathF.Abs(source[s + axis]);

                dest[t] = source[s + a] * inv;
                dest[t + 1] = source[s + b] * inv;
            }

            return target;
        }

        protected void UpdateMaterials()
        {
            var tex = _provider!.GetLightField().Textures;

            _grid = _provider!.Baker.GridDesc;

            _fieldMat.VoxelSize = _grid.VoxelSize;
            _fieldMat.Origin = _grid.Origin;
            _fieldMat.Size = _grid.Size;
            _fieldMat.Textures = tex;
            _fieldMat.Invalidate();

            _fieldView.InstanceCount = _grid.Size.Area();

            PbrMaterial.SHADER.UseLightField = true;
            PbrMaterial.SHADER.NotifyChanged(ChangeType.Render);

            foreach (var light in _host!.Descendants<Light>())
                light.IsVisible = false;

            Log.Info(this, "Texture loaded");
        }

        void UpdateMeshView()
        {
            var faces = new List<GpuVoxelFaceInstance>();

            foreach (var child in _host!.Children)
            {
                if (child.TryComponent<LightFieldReceiver>(out var rec) && rec.IsOccluder)
                {
                    faces.AddRange(rec.Voxels!.Select(a => new GpuVoxelFaceInstance
                    {
                        Face = a.Face,
                        Pos = a.Cell,
                        BaseColor = a.BaseColor,
                        Metallic = a.Metallic,
                        Normal = a.Normal,
                        Roughness = a.Roughness,
                    }));
                }
            }

            _meshMat.GridDesc = _grid;
            _meshMat.LoadFaces(faces.ToArray());
            _meshView.InstanceCount = faces.Count;

            _meshMat.Invalidate();

            if (_meshView.Parent == null)
                _host!.Scene!.AddChild(_meshView);
        }

        [Action]
        public async Task Backe()
        {
            Init();

            UpdateParams();

            await _provider!.RebuildAsync();

            UpdateMeshView();

            UpdateMaterials();
        }

        [Action]
        public async Task Extract()
        {
            Log.Info(this, "Extract light field");

            UpdateParams();

            await _provider!.ExtractAsync();

            UpdateMaterials();
        }

        [Action]
        public void CraeteRay()
        {
            Init();

            UpdateParams();

            _ray?.Dispose();
            _ray = _provider!.Baker!.CreateRayMarcher();

            _ray.Create(new VoxelLightRay
            {
                Position = RayOrigin,
                Direction = RayDir.Normalize(),
                Energy = Vector3.One * RayEnergy,
                Falloff = new LightCurve
                {
                    Type = LightFallOff,
                    Factor = 1f,
                    Range = LightRange
                },
                Recovery = new LightCurve
                {
                    Type = LightCurveType.Quadratic,
                    Factor = 1f,
                    Range = RecoveryRange
                }
            });
        }

        [Action]
        public void Step()
        {
            if (_ray == null)
                return;

            var res = _ray.Step();

            var state = _ray.GetState();

            var fillFaces = 0;

            foreach (var face in state.LastVoxel.Faces)
            {
                if (face.TriangleId > 0)
                    fillFaces++;
            }

            Log.Debug(this, "{4}{0} / {1}  E: {2} - F: {3}", state.Cell, state.LastAffectedVoxel, state.Energy.Length(), fillFaces, !res ? "[DEAD] " : "");

            _curVoxel.WorldPosition = new Vector3(
                _grid.Origin.X + (state.Cell.X + 0.5f) * _grid.VoxelSize,
                _grid.Origin.Y + (state.Cell.Y + 0.5f) * _grid.VoxelSize,
                _grid.Origin.Z + (state.Cell.Z + 0.5f) * _grid.VoxelSize);
        }

        [Action]
        public void Export()
        {
            if (StorePath == null)
                return;

            var path = Path.Combine(StorePath, "LightField");

            _provider!.Export(path);
        }

        [Action]
        public void Import()
        {
            if (StorePath == null)
                return;

            Init();

            var path = Path.Combine(StorePath, "LightField");

            _provider!.Import(path);

            UpdateMaterials();
        }

        [Action]
        public void CopyPreset()
        {
            UpdateParams();

            var clip = Context.Require<IClipboard>();

            var json = JsonSerializer.Serialize(_provider!.Baker!.Params, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
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

        [Category("Trace")]
        public LightTrackMode TrackMode { get; set; }

        [Category("Trace")]
        public Vector3 RayOrigin { get; set; }

        [Category("Trace")]
        [Range(-1, 1, 0.01f)]
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
        public LightCurveType LightFallOff { get; set; }

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
    }
}

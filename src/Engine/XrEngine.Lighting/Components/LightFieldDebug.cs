using CanvasUI;
using Common.Interop;
using Silk.NET.Core.Native;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine.OpenGL;
using XrEngine.UI;
using XrMath;

namespace XrEngine.Lighting
{
    [StateManager(StateManagerMode.Auto)]
    public class LightFieldDebug : BaseComponent<TriangleMesh>, ILightFieldProvider, IDrawGizmos
    {

        readonly TriangleMesh _meshView;
        readonly TriangleMesh _fieldView;
        readonly CanvasView2D _canvas;
        readonly MeshVoxelMaterial _meshMat;
        readonly LightFieldViewMaterial _fieldMat;
        readonly TriangleMesh _curVoxel;
        readonly FieldPhongMaterial _finalMat;
        readonly VoxelLightBaker _backer;
        readonly bool _isReadMode;

        IGlContext? _workerCtx;

        VoxelRayMarcher? _ray;

        LightFieldData? _data;
        TriangleMesh[]? _walls;

        VoxelGridDesc _gridDesc;
        GpuMeshVoxelizer? _gpuVoxelizer;

        IList<GpuVoxelFaceData>? _faces;
        VoxelLightFieldView _field;
        IList<Texture3D>? _textures;
        TextureData[]? _texData;

        bool _useWorker;

        public LightFieldDebug(VoxelGridDesc gridDesc, bool isReadMode)
        {
            _isReadMode = isReadMode;

            _gridDesc = gridDesc;   

            _backer = new VoxelLightBaker();

            _finalMat = new FieldPhongMaterial();

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

            DiffuseStrength = 1;

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

        protected void CreateWalls()
        {
            var cell = _gridDesc.VoxelSize;
            var size = _gridDesc.Size;
            var origin = _gridDesc.Origin;
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

            if (_host.Materials.Count == 1)
                _host.Materials.Add(_finalMat);

            foreach (var wall in _walls!)
            {
                if (wall.Parent == null)
                    _host.Scene.AddChild(wall);
            }
     
        }


        public void DrawGizmos(Canvas3D canvas)
        {
            canvas.State.Color = "#ff0000";

            canvas.DrawLine(RayOrigin, RayOrigin + RayDir.Normalize() * 3f);
        }

        private void OnDraw(ScreenCanvas obj)
        {
            obj.DrawCube(_curVoxel.WorldPosition, new Vector3(_gridDesc.VoxelSize),
                         "#ff0000", 3);
        }

        protected void UpdateParams()
        {
            _backer.SetParams(new VoxelLightBakeParams
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


        protected void LoadTextures()
        {
            _fieldMat.VoxelSize = _backer.GridDesc.VoxelSize;
            _fieldMat.Origin = _backer.GridDesc.Origin;
            _fieldMat.Size = _backer.GridDesc.Size;
            _fieldMat.Textures = _textures;
            _fieldMat.Invalidate();

            _finalMat.IsEnabled = false;
            _finalMat.VoxelSize = _backer.GridDesc.VoxelSize;
            _finalMat.Origin = _backer.GridDesc.Origin;
            _finalMat.Size = _backer.GridDesc.Size;
            _finalMat.Textures = _textures;
            _finalMat.Invalidate();

            PbrMaterial.SHADER.UseLightField = true;
            PbrMaterial.SHADER.NotifyChanged(ChangeType.Render);

            ((PbrMaterial)_host!.Materials[0]).UseLightField = UseLightFieldMode.Self;
            ((PbrMaterial)_host!.Materials[0]).NotifyChanged(ChangeType.Render);

            foreach (var light in _host.Scene!.Descendants<Light>())
                light.IsVisible = false;

            Log.Info(this, "Texture loaded");
        }

        [Action]
        public void Apply()
        {
            Init();

            Log.Info(this, "Begin");

            _gpuVoxelizer ??= new GpuMeshVoxelizer(OpenGLRender.Current!.GL);

   
            foreach (var wall in _walls!)
                wall.Component<LightFieldReceiver>().IsOccluder = TrackMode == LightTrackMode.Full;

            _gpuVoxelizer.SetGrid(_gridDesc);

            if (_useWorker)
            {
                _workerCtx ??= Context.Require<IGlContextProvider>().CreateShared();

                _ = Task.Run(async () =>
                {
                    _workerCtx.Take();

                    OpenGLRender.Current ??= new OpenGLRender(_workerCtx.Gl);

                    Log.Info(this, "Begin voxelize");

                    XrEngine.EngineNativeLib.RdcStartFrameCapture();

                    _faces = _gpuVoxelizer.Voxelize([_host!, .. _walls]);

                    Log.Info(this, "End voxelize");

                    XrEngine.EngineNativeLib.RdcEndFrameCapture(true);

                    _workerCtx.Release();

                    _meshMat.Target = _host;
                    _meshMat.GridDesc = _gridDesc;
                    _meshMat.FaceInstances = _faces!.Select(a => new GpuVoxelFaceInstance
                    {
                        Face = a.Face,
                        Pos = a.Cell,
                        TriangleId = 1,
                    }).ToArray();

                    _meshView.InstanceCount = _meshMat.FaceInstances.Length;

                    await EngineApp.MainThread;

                    _meshMat.Invalidate();

                });

            }
            else
            {
                Log.Info(this, "Begin voxelize");

                XrEngine.EngineNativeLib.RdcStartFrameCapture();

                _faces = _gpuVoxelizer.Voxelize([_host!, .. _walls]);

                Log.Info(this, "End voxelize");

                XrEngine.EngineNativeLib.RdcEndFrameCapture(true);

                _meshMat.Target = _host;
                _meshMat.GridDesc = _gridDesc;
                _meshMat.FaceInstances = _faces!.Select(a => new GpuVoxelFaceInstance
                {
                    Face = a.Face,
                    Pos = a.Cell,
                    TriangleId = 1,
                }).ToArray();

                _meshView.InstanceCount = _meshMat.FaceInstances.Length;

                _meshMat.Invalidate();
            }

            if (_meshView.Parent == null)
                _host!.Scene!.AddChild(_meshView);

            Log.Info(this, "Done");
        }


        [Action]
        public void Backe()
        {
            if (_faces == null)
                return;

            Init();

            UpdateParams();

            _backer.ClearLightField();
            _backer.ClearScene();
            _backer.SetGrid(_gridDesc);

            _backer.AddMesh(_faces.ToArray());

            Log.Info(this, "Backing point light");

            using var lightMap = _backer.BakeLight(new VoxPointLight
            {
                Color = new Vector3(1, 1, 1),
                Intensity = RayEnergy,
                Position = RayOrigin,
                Falloff = new LightCurve
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



        [Action]
        public void Extract()
        {
            Log.Info(this, "Extract light field");

            UpdateParams();

            _field = _backer.GetLightField(true);

            _fieldView.InstanceCount = _field.Size.Area();

            if (_fieldMat.Textures != null)
            {
                foreach (var tex in _fieldMat.Textures)
                    tex.Dispose();
            }

            _textures = _backer.CreateTextures();

            _texData = _textures.Select(a => a.Data![0]).ToArray();

            LoadTextures();
        }

        [Action]
        public void CraeteRay()
        {
            if (_faces == null)
                return;

            Init();

            _backer.ClearLightField();
            _backer.ClearScene();
            _backer.SetGrid(_gridDesc);

            UpdateParams();

            _backer.AddMesh(_faces.ToArray());

            _ray?.Dispose();
            _ray = _backer.CreateRayMarcher();

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

            int fillFaces = 0;

            foreach (var face in state.LastVoxel.Faces)
            {
                if (face.TriangleId > 0)
                    fillFaces++;
            }

            Log.Debug(this, "{4}{0} / {1}  E: {2} - F: {3}", state.Cell, state.LastAffectedVoxel, state.Energy.Length(), fillFaces, !res ? "[DEAD] " : "");

            _curVoxel.WorldPosition = new Vector3(
                _gridDesc.Origin.X + (state.Cell.X + 0.5f) * _gridDesc.VoxelSize,
                _gridDesc.Origin.Y + (state.Cell.Y + 0.5f) * _gridDesc.VoxelSize,
                _gridDesc.Origin.Z + (state.Cell.Z + 0.5f) * _gridDesc.VoxelSize);
        }

        [Action]
        public void Export()
        {
            if (_texData == null || StorePath == null)
                return;

            var path = Path.Combine(StorePath, "LightField");
            Directory.CreateDirectory(path);

            var writer = PvrTranscoder.Instance;

            for (var i = 0; i < _texData.Length; i++)
            {
                using var fs = File.OpenWrite(Path.Combine(path, $"Tex_{i}.pvr"));

                writer.SaveTexture(fs, [_texData[i]]);
            }
        }

        [Action]
        public void Import()
        {
            if (StorePath == null)
                return;

            var path = Path.Combine(StorePath, "LightField");

            if (!Directory.Exists(path))
                return;

            var reader = PvrTranscoder.Instance;

            var textures = new List<Texture3D>();

            var files = Directory.GetFiles(path, "*.pvr")
                .OrderBy(a => int.Parse(Path.GetFileNameWithoutExtension(a).Split('_')[1]))
                .ToArray();

            bool packDir = false;

            foreach (var file in files)
            {
                using var fs = File.OpenRead(file);
                var data = reader.LoadTexture(fs);

                TextureFormat format;
                TextureType type = TextureType.Unspecified;

                if ((textures.Count % 2) == 0)
                    format = TextureFormat.Rgb9e5Float;
                else
                {
                    type = TextureType.NormalMap;
                    if (packDir)
                    {
                        var face = textures.Count / 2;

                        format = TextureFormat.RgFloat16;

                        data[0].Data = ExtractFaceDirection2Texture(data[0].Data!,
                            (int)data[0].Width,
                            (int)data[0].Height,
                            (int)data[0].Depth,
                            face, 3);

                        data[0].Format = TextureFormat.RgFloat32;
                    }
                    else
                        format = TextureFormat.RgbFloat16;
                }


                var tex = new Texture3D()
                {
                    Format = format,
                    MipLevelCount = 0,
                    MinFilter = ScaleFilter.Nearest,
                    MagFilter = ScaleFilter.Linear,
                    Type = type
                };

                tex.LoadData(data);

                textures.Add(tex);
            }

            _textures = textures.ToArray();
            _texData = null;

            Init();
            LoadTextures();
        }


        [Action]
        public void CopyPreset()
        {
            UpdateParams();

            var clip = Context.Require<IClipboard>();

            var json = JsonSerializer.Serialize(_backer.Params, new JsonSerializerOptions
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

        public LightFieldData GetLightField()
        {
            _data ??= new LightFieldData();

            _data.Textures = _textures;
            _data.DiffuseStrength = DiffuseStrength;
            _data.SpecularStrength = SpecularStrength;
            _data.Origin = _gridDesc.Origin;
            _data.Size = _gridDesc.Size;
            _data.VoxelSize = _gridDesc.VoxelSize;
            _data.UseAllFaces = true;

            return _data;
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

        [Category("Misc")]
        [Range(0,1, 0.01f)]
        public float DiffuseStrength { get; set; }

        [Category("Misc")]
        [Range(0, 1, 0.01f)]
        public float SpecularStrength { get; set; }

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

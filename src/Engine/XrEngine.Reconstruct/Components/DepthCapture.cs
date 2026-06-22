#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
using OpenXr.Framework;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using XrEngine.Devices;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;
using glTFLoader.Schema;

namespace XrEngine.Reconstruct
{
    public enum DepthSnapeshotMode
    {
        Read,
        Capture,
        Record
    }

    public class CaptureFrame : BaseComponent<TriangleMesh>, ISelectionHandler
    {
        public CaptureFrame()
        {
            Exposure = 0;
        }

        [Action]
        public void GoToPose()
        {
            Debug.Assert(_host?.Scene?.ActiveCamera != null);
            Debug.Assert(Meta != null);

            _host.Scene.ActiveCamera.View = Meta.CameraView;
            _host.Scene.ActiveCamera.Projection = Meta.CameraProj;
            _host.Scene.ActiveCamera.Far = 100;
        }

        public void OnSelected(Object3D obj, bool isSelected)
        {
            _host!.Materials[1].IsEnabled = isSelected && ShowWireFrame;
        }

        public Texture2D? Texture { get; set; }

        public bool ShowWireFrame { get; set; }

        [Range(-1, 1, 0.01f)]
        public float Exposure { get; set; }

        public DepthCapture.DepthFrameMeta? Meta { get; set; }

        public IMemoryBuffer<byte>? ColorData;
    }

    public class DepthCapture : Behavior<Group3D>
    {
        public class DepthFrame
        {
            public TriangleMesh? Mesh;

            public Texture2D? CameraTexture;

            public Matrix4x4 DepthView;

            public Matrix4x4 DepthProj;

            public Matrix4x4 CameraView;

            public Matrix4x4 CameraProj;

            public long DepthXrTime;

            public long CameraXrTime;

            public long FrameXrTime;
        }

        public class DepthFrameMeta
        {
            public int Frame;

            public int ColorWidth;
            public int ColorHeight;

            public int DepthWidth;
            public int DepthHeight;

            public int GridWidth;
            public int GridHeight;

            public Matrix4x4 DepthView;
            public Matrix4x4 DepthProj;

            public Matrix4x4 CameraView;
            public Matrix4x4 CameraProj;

            public long DepthXrTime;
            public long CameraXrTime;
            public long FrameXrTime;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };

        private int _frameIndex;
        private SplatMesh? _splatMesh;
        private CameraController? _capture;
        private EnvDepthMesh? _envDepth;
        private XrBoolInput? _captureBtn;
        private XrBoolInput? _deleteBtn;
        private IMemoryBuffer<byte>[]? _buffers;
        private readonly DepthGeometryGenerator _generator;
        private string? _lastPath;
        private TriangleMesh? _recMesh;
        private Texture2D? _colorArrayTex;
        private GlTexture? _depthTex;
        private Texture2D? _atlasTex;
        private readonly DepthSnapeshotMode _mode;
        private readonly string _sessionPath;
        private readonly List<DepthFrame> _frames = [];

        public DepthCapture(DepthSnapeshotMode mode)
        {
            if (mode == DepthSnapeshotMode.Record)
            {
                var root = Path.Combine(XrPlatform.Current!.SharedPath, "DepthSnapshots");
                _sessionPath = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                Directory.CreateDirectory(_sessionPath);
            }
            else
            {
                _sessionPath = "";
            }

            _mode = mode;

            GridSize = 300;
            DepthMapSize = 300;
            UseMeshCache = true;
            BuildAtlas = true;
            ComputeIndices = true;
            UseDepthOcclusion = true;
            SolveExposure = true;
            Optimize = true;

            MeshParams = new();
            GeneratorParams = new();
            ExposeParams = new();
            ProjParams = new();

            _generator = new DepthGeometryGenerator(GridSize, GridSize);
        }

        protected override void Start(RenderContext ctx)
        {
            if (_mode == DepthSnapeshotMode.Read)
                return;

            Debug.Assert(_host?.Scene != null);

            if (!_host.Scene.TryComponent(out _capture))
            {
                _capture = new CameraController();
                _host.Scene.AddComponent(_capture);
            }

            _envDepth = _host.Scene.Descendants<EnvDepthMesh>().FirstOrDefault();
            _envDepth ??= _host.AddChild(new EnvDepthMesh(new Size2I(300, 300)));

            _ = _capture.StartCameraAsync(OculusCameras.Left);
        }

        private unsafe void SaveTextureRaw(Texture2D texture, TextureFormat format, int bytesPerPixel, string path)
        {
            _buffers ??= [MemoryBuffer.Create<byte>(16), MemoryBuffer.Create<byte>(16)];

            OpenGLRender.Current!.ReadTexture(texture, format, 0, 0, _buffers);

            var size = checked((int)(texture.Width * texture.Height * bytesPerPixel));

            using var pTex = _buffers[0].MemoryLock();
            using var file = File.Create(path);

            file.Write(new ReadOnlySpan<byte>(pTex.Data, size));
        }

        private Texture2D LoadColorTextureRaw(string path, int width, int height, string name)
        {
            var texture = CreateTexture((uint)width, (uint)height);

            texture.LoadData(new TextureData
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = TextureFormat.Rgba32,
                Data = MemoryBuffer.Create(File.ReadAllBytes(path))
            });

            return texture;
        }

        private void SaveFrame(DepthFrame frame, DepthFrameMeta meta)
        {
            var framePath = Path.Combine(_sessionPath, $"frame_{meta.Frame:000000}");
            Directory.CreateDirectory(framePath);

            File.WriteAllText(
                Path.Combine(framePath, "meta.json"),
                JsonSerializer.Serialize(meta, _jsonOptions));

            SaveTextureRaw(
                frame.CameraTexture!,
                TextureFormat.Rgba32,
                4,
                Path.Combine(framePath, "color_rgba.raw"));

            var mat = (EnvDepthMaterial)_envDepth!.Materials[0];

            SaveTextureRaw(
                mat.LastTexture!,
                TextureFormat.GrayInt16,
                2,
                Path.Combine(framePath, "depth_u16.raw"));
        }

        protected Material CreateMaterial(Texture2D texture)
        {
            if (_mode == DepthSnapeshotMode.Read)
            {
                return new GridMaterial
                {
                    Priority = -1000 + _frames.Count,
                    WriteDepth = true,
                    UseDepth = true,
                    Alpha = AlphaMode.Blend,
                    Texture = texture
                };
            }

            return new TextureMaterial(texture)
            {
                Priority = -1000 + _frames.Count,
                WriteDepth = true,
                UseDepth = false,
                Color = new Color(1, 1, 1, 1f),
                Alpha = AlphaMode.Blend
            };
        }

        protected Texture2D CreateTexture(uint width, uint height)
        {
            return new Texture2D
            {
                Width = width,
                Height = height,
                MipLevelCount = 5,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.LinearMipmapLinear,
                Format = TextureFormat.Rgba32
            };
        }

        private async Task<IMemoryBuffer<byte>> GenerateDepthAsync(Matrix4x4 cameraViewProj)
        {
            await EngineApp.RenderThread;

            var gl = OpenGLRender.Current?.GL;
            if (gl == null)
                throw new InvalidOperationException();

            Debug.Assert(_recMesh != null);

            _depthTex = GlTempAllocator.StaticTexture(
                gl,
                (uint)DepthMapSize,
                (uint)DepthMapSize,
                1,
                TextureFormat.Depth16);

            if (_depthTex.MinFilter != TextureMinFilter.Nearest || _depthTex.MagFilter != TextureMagFilter.Nearest)
            {
                _depthTex.MinFilter = TextureMinFilter.Nearest;
                _depthTex.MagFilter = TextureMagFilter.Nearest;
                _depthTex.Update();
            }

            GlState.Current!.SetView(new Rect2I
            {
                Width = _depthTex.Width,
                Height = _depthTex.Height
            });

            var fb = GlImageProc.PrepareFrameBuffer(gl, null, (IGlRenderAttachment)_depthTex);

            try
            {
                var prog = GlImageProc.LoadProgram(gl, "empty.frag", "basic.vert");

                prog.Use();
                prog.SetUniform("uViewProj", cameraViewProj);
                prog.SetUniform("uWorldMatrix", _recMesh.WorldMatrix);

                GlState.Current.SetWriteDepth(true);
                GlState.Current.SetUseDepth(true);
                GlState.Current.SetWriteColor(false);
                GlState.Current.SetClearColor(Color.Transparent);
                GlState.Current.SetAlphaMode(AlphaMode.Opaque);

                gl.Clear(ClearBufferMask.DepthBufferBit);

                var vertexHandler = _recMesh.Geometry!.GetGlResource(a => GlVertexSourceHandle.Create(gl, _recMesh));

                if (vertexHandler.NeedUpdate)
                    vertexHandler.Update();

                vertexHandler.Bind();
                vertexHandler.Draw();
            }
            finally
            {
                fb.Unbind();
                GlState.Current!.SetWriteColor(true);
            }

            var data = GlImageProc.Read(_depthTex, TextureFormat.GrayInt16);

            Debug.Assert(data != null && data.Count == 1);

            return data[0].Data!;
        }

        [Action]
        public async Task GenerateMeshAsync()
        {
            Debug.Assert(_lastPath != null);
            Debug.Assert(_colorArrayTex != null);
            Debug.Assert(_host != null);

            var rec = new VoxelMeshReconstructor();
            rec.SetParams(MeshParams);

            var proj = new MeshTextureProjection();
            proj.SetParams(ProjParams);

            var colorFrames = new List<ColorProjectionFrame>();
            var cacheName = Path.Combine(_lastPath, "reconstruct.obj");
            var skipRec = File.Exists(cacheName) && UseMeshCache;

            if (skipRec)
            {
                Log.Info(this, "Load geometry");
                _recMesh = AssetLoader.Instance.Load<TriangleMesh>(cacheName);
            }
            else
            {
                _recMesh ??= new TriangleMesh(new Geometry3D());
            }

            Debug.Assert(_recMesh.Geometry != null);

            _recMesh.Geometry.ActiveComponents =
                VertexComponent.Normal |
                VertexComponent.Position |
                VertexComponent.UV0 |
                VertexComponent.UV1 |
                VertexComponent.Tangent;


            var colorData = new List<IMemoryBuffer<byte>>();

            var colorSize = new Size2I();

            foreach (var mesh in _host.Children.OfType<TriangleMesh>())
            {
                if (!mesh.TryComponent<CaptureFrame>(out var frame))
                    continue;

                var meta = frame.Meta!;
                var cameraViewProj = meta.CameraView * meta.CameraProj;
                
                colorSize.Width = (uint)meta.ColorWidth;
                colorSize.Height = (uint)meta.ColorHeight;

                if (!skipRec)
                {
                    Log.Info(this, "Feed frame {0}", meta.Frame);
                    rec.FeedFrame(mesh.Geometry!);
                }

                colorFrames.Add(new ColorProjectionFrame(
                    meta.Frame,
                    meta.CameraView.Invert().Translation,
                    cameraViewProj));

                colorData.Add(frame.ColorData!);
            }

            if (!skipRec)
            {
                Log.Info(this, "Extracting mesh");

                rec.ExtractMesh(_recMesh.Geometry);

                var objWriter = new ObjWriter();
                objWriter.Add(_recMesh);

                File.WriteAllText(cacheName, objWriter.Text());
            }

            Log.Warn(this, "Mesh extracted {0} - {1}", _recMesh.Geometry.Vertices!.Length, _recMesh.Geometry.Indices!.Length);

            if (UseDepthOcclusion)
            {
                foreach (var frame in colorFrames)
                {
                    Log.Info(this, "Generate deph {0}", frame.ImageIndex);

                    if (frame.ImageIndex == 0)
                        EngineNativeLib.RdcStartFrameCapture();

                    frame.DepthMap = await GenerateDepthAsync(frame.ViewProj);
                    frame.DepthWidth = DepthMapSize;
                    frame.DepthHeight = DepthMapSize;

                    if (frame.ImageIndex == 0)
                        EngineNativeLib.RdcEndFrameCapture(false);
                }
            }

            float[] exposures = [];

            Log.Info(this, "Color projection");

            proj.Project(_recMesh.Geometry, colorFrames);

            if (SolveExposure)
            {
                Log.Info(this, "Solving exposure");
      
                var solver = new FrameExposureSolver();

                solver.SetParams(ExposeParams);

                exposures = solver.Compute(
                    _recMesh.Geometry,
                    colorData.ToArray(),
                    (int)colorSize.Width,
                    (int)colorSize.Height);
            }

            _recMesh.Materials.Clear();

            if (BuildAtlas)
            {
                Log.Info(this, "Bulding atlas");

                var builder = new TextureAtlasLayoutBuilder
                {
                    TextureWidth = 1280,
                    TextureHeight = 1280,
                    SourceTextureCount = _host.Children.Count,
                    Padding = 2,
                    SourceBorderPixels = 2,
                    BytesPerPixel = 4
                };

                _atlasTex?.Dispose();

                _atlasTex = await builder.GenerateAtlasTextureAsync([_recMesh.Geometry], _colorArrayTex, exposures);

                _recMesh.Materials.Add(new TextureMaterial(_atlasTex));
            }
            else
            {
                _recMesh.Materials.Add(new MultiTextureMaterial
                {
                    Texture = _colorArrayTex,
                    Exposure = exposures
                });
            }

            if (ComputeIndices)
            {
                Log.Info(this, "Compute Indices");
                _recMesh.Geometry.ComputeIndices();
            }
            else
                _recMesh.Geometry.NotifyChanged(ChangeType.Geometry);

            if (Optimize)
            {
                Log.Info(this, "Optmize");
                MeshOptimizer.OptimizeVertexCache(_recMesh.Geometry!);
                MeshOptimizer.OptimizeOverdraw(_recMesh.Geometry!, 1.05f);
                MeshOptimizer.OptimizeVertexFetch(_recMesh.Geometry!);
            }

            Log.Warn(this, "Done {0} - {1}", _recMesh.Geometry.Vertices!.Length, _recMesh.Geometry.Indices!.Length);

            if (_recMesh.Parent == null)
                _host.Scene!.AddChild(_recMesh);

            _recMesh.NotifyChanged(ChangeType.Geometry | ChangeType.Material);
        }

        [Action]
        public void Rebuild()
        {
            if (_lastPath == null)
                return;

            Load(_lastPath, true);
        }

        public unsafe void Load(
            string path,
            bool clear = false,
            float cleanupMargin = -0.01f)
        {
            _lastPath = path;

            if (clear)
            {
                foreach (var frame in _frames)
                {
                    frame.CameraTexture?.Dispose();
                    frame.Mesh?.Dispose();
                }

                _frames.Clear();
                _host!.Clear();
            }

            var frameDirs = Directory.GetDirectories(path)
                .OrderBy(a => a)
                .ToArray();

            var splats = new List<SplatData>();
            var texArrayData = new List<TextureData>();

            _generator.SetParams(GeneratorParams);

            foreach (var frameDir in frameDirs)
            {
                var metaPath = Path.Combine(frameDir, "meta.json");
                var depthPath = Path.Combine(frameDir, "depth_u16.raw");
                var colorPath = Path.Combine(frameDir, "color_rgba.raw");

                if (!File.Exists(metaPath) ||
                    !File.Exists(depthPath) ||
                    !File.Exists(colorPath))
                {
                    continue;
                }

                var meta = JsonSerializer.Deserialize<DepthFrameMeta>(
                    File.ReadAllText(metaPath),
                    _jsonOptions)!;

                Log.Info(this, "Loading frame {0}", meta.Frame);

                var colorViewProj = meta.CameraView * meta.CameraProj;

                if (Clip && splats.Count > 0)
                    RemoveSplatsInsideFrame(splats, colorViewProj, cleanupMargin);

                var depthBytes = File.ReadAllBytes(depthPath);

                Geometry3D geometry;

                fixed (byte* pBytes = depthBytes)
                {
                    geometry = _generator.CreateGeometry(
                        (ushort*)pBytes,
                        meta.DepthWidth,
                        meta.DepthHeight,
                        Matrix4x4.Invert(meta.DepthView * meta.DepthProj, out var inv)
                            ? inv
                            : Matrix4x4.Identity,
                        colorViewProj);
                }

                var mesh = new TriangleMesh(geometry)
                {
                    Name = $"Frame {meta.Frame}"
                };

                var colorTexture = LoadColorTextureRaw(
                    colorPath,
                    meta.ColorWidth,
                    meta.ColorHeight,
                    $"Tex Frame {meta.Frame}");

                var colorData = colorTexture.Data![0];

                texArrayData.Add(new TextureData
                {
                    Width = colorData.Width,
                    Height = colorData.Height,
                    Data = colorData.Data,
                    Layer = (uint)meta.Frame,
                    Format = colorData.Format
                });

                mesh.Materials.Add(CreateMaterial(colorTexture));

                mesh.Materials.Add(new WireframeMaterial
                {
                    Color = new Color(1, 1, 1, 1),
                    IsEnabled = false
                });

                mesh.AddComponent(new CaptureFrame
                {
                    Meta = meta,
                    Texture = colorTexture,
                    ColorData = colorData.Data
                });

                if (SplatMode)
                {
                    DepthGridSplatBuilder.CreateSplats(
                        splats,
                        geometry,
                        colorData.Data!,
                        (int)colorTexture.Width,
                        (int)colorTexture.Height);
                }

                _frames.Add(new DepthFrame
                {
                    Mesh = mesh,
                    CameraTexture = colorTexture,

                    CameraProj = meta.CameraProj,
                    CameraView = meta.CameraView,

                    DepthView = meta.DepthView,
                    DepthProj = meta.DepthProj,

                    CameraXrTime = meta.CameraXrTime,
                    DepthXrTime = meta.DepthXrTime,
                    FrameXrTime = meta.FrameXrTime
                });

                if (!SplatMode)
                    _host!.AddChild(mesh);
            }

            _colorArrayTex = new Texture2D
            {
                MinFilter = ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                MipLevelCount = 0
            };

            _colorArrayTex.LoadData(texArrayData);

            _frameIndex = _frames.Count;

            if (SplatMode)
            {
                _splatMesh = new SplatMesh(splats.ToArray());
                _host!.AddChild(_splatMesh);
            }

            Log.Info(this, "Done!");
        }

        private void RemoveSplatsInsideFrame(List<SplatData> splats, Matrix4x4 colorViewProj, float cleanupMargin)
        {
            var write = 0;

            for (var read = 0; read < splats.Count; read++)
            {
                var splat = splats[read];
                var clip = Vector4.Transform(new Vector4(splat.Position, 1.0f), colorViewProj);
                var remove = false;

                if (clip.W > 0.00001f)
                {
                    var p = clip / clip.W;

                    remove =
                        p.X >= -1.0f - cleanupMargin && p.X <= 1.0f + cleanupMargin &&
                        p.Y >= -1.0f - cleanupMargin && p.Y <= 1.0f + cleanupMargin &&
                        p.Z >= -1.0f - cleanupMargin && p.Z <= 1.0f + cleanupMargin;
                }

                if (!remove)
                    splats[write++] = splat;
            }

            if (write < splats.Count)
                splats.RemoveRange(write, splats.Count - write);
        }

        public DepthFrame? CreateSnapeshot()
        {
            var camera = _capture!.GetCameraStatus(OculusCameras.Left);

            if (!camera.IsActive)
                return null;

            var cameraTime = camera.FrameTime;
            var cameraWorld = camera.Pose?.ToMatrix() ?? Matrix4x4.Identity;

            Matrix4x4.Invert(cameraWorld, out var cameraView);

            var cameraProj = camera.Proj!.Value;
            var cameraViewProj = cameraView * cameraProj;

            var frozenMesh = _envDepth!.Freeze(cameraViewProj);

            if (frozenMesh == null)
                return null;

            var mat = (EnvDepthMaterial)_envDepth.Materials[0];
            var frameTexture = CreateTexture(camera.Texture!.Width, camera.Texture.Height);

            GlImageProc.CopyColor(camera.Texture!.ToGlTexture(), frameTexture.ToGlTexture());

            frozenMesh.Materials.Add(CreateMaterial(frameTexture));

            var frame = new DepthFrame
            {
                CameraProj = cameraProj,
                CameraView = cameraView,
                CameraXrTime = cameraTime,

                Mesh = frozenMesh,
                CameraTexture = frameTexture,

                DepthView = mat.DepthCamera.Eyes![0].View,
                DepthProj = mat.DepthCamera.Eyes![0].Projection,
                DepthXrTime = mat.LastFrameTime,

                FrameXrTime = XrApp.Current!.FramePredictedDisplayTime
            };

            if (_mode == DepthSnapeshotMode.Record)
            {
                var size = ((Grid3D)_envDepth.Geometry!).Size;

                var meta = new DepthFrameMeta
                {
                    Frame = _frameIndex++,

                    ColorWidth = (int)camera.Texture.Width,
                    ColorHeight = (int)camera.Texture.Height,

                    DepthWidth = (int)mat.LastTexture!.Width,
                    DepthHeight = (int)mat.LastTexture.Height,

                    GridWidth = (int)size.Width,
                    GridHeight = (int)size.Height,

                    CameraProj = cameraProj,
                    CameraView = cameraView,

                    DepthView = mat.DepthCamera.Eyes![0].View,
                    DepthProj = mat.DepthCamera.Eyes![0].Projection,

                    CameraXrTime = cameraTime,
                    DepthXrTime = mat.LastFrameTime,
                    FrameXrTime = frame.FrameXrTime
                };

                SaveFrame(frame, meta);
            }

            return frame;
        }

        public void DeleteLast()
        {
            if (_frames.Count == 0)
                return;

            var lastFrame = _frames[^1];

            _frames.RemoveAt(_frames.Count - 1);

            lastFrame.CameraTexture!.Dispose();
            lastFrame.Mesh!.Dispose();
        }

        protected override void Update(RenderContext ctx)
        {
            if (_mode == DepthSnapeshotMode.Read)
            {
                foreach (var mesh in _host!.Children.OfType<TriangleMesh>())
                {
                    if (!mesh.TryComponent<CaptureFrame>(out var frame))
                        continue;

                    if (mesh.Materials[0] is GridMaterial mat)
                        mat.Exposure = frame.Exposure;
                }

                return;
            }

            Debug.Assert(_captureBtn != null && _deleteBtn != null);

            if (_captureBtn.IsChanged && _captureBtn.Value)
            {
                var frame = CreateSnapeshot();

                if (frame != null)
                {
                    _frames.Add(frame);
                    _host!.AddChild(frame.Mesh!);
                }
            }

            if (_deleteBtn.IsChanged && _deleteBtn.Value)
                DeleteLast();
        }

        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            _captureBtn = input.Right!.Button!.AClick!;
            _deleteBtn = input.Right!.Button!.BClick!;
        }

        public string SessionPath => _sessionPath;

        public int DepthMapSize { get; set; }

        public int GridSize { get; set; }

        public bool SolveExposure { get; set; }

        public bool SplatMode { get; set; }

        public bool Optimize { get; set; }

        public bool Clip { get; set; }

        public bool UseMeshCache { get; set; }

        public bool UseDepthOcclusion { get; set; }

        public bool BuildAtlas { get; set; }

        public bool ComputeIndices { get; set; }

        public VoxelMeshReconstructorParams MeshParams { get; set; }

        public DepthGeometryGeneratorParams GeneratorParams { get; set; }

        public FrameExposureSolverParams ExposeParams { get; set; }

        public MeshTextureProjectionParams ProjParams { get; set; }

    }
}
using Common.Interop;
using OpenXr.Framework;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using XrEngine.Abstraction;
using XrEngine.Devices;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;

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
            _host!.Materials[1].IsEnabled = isSelected;
        }

        public Texture2D? Texture { get;  set; }

        public DepthSnapeshot.DepthFrameMeta? Meta { get;  set; }
    }

    public class DepthSnapeshot : Behavior<Group3D>
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

        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };


        int _frameIndex;
        SplatMesh? _splatMesh;
        CameraController? _capture;
        EnvDepthMesh? _envDepth;
        XrBoolInput? _captureBtn;
        XrBoolInput? _deleteBtn;
        IMemoryBuffer<byte>[]? _buffers;
        readonly DepthSnapeshotMode _mode;
        readonly string _sessionPath;
        readonly List<DepthFrame> _frames = [];


        public DepthSnapeshot(DepthSnapeshotMode mode)
        {
            if (mode == DepthSnapeshotMode.Record)
            {
                var root = Path.Combine(XrPlatform.Current!.SharedPath, "DepthSnapshots");
                _sessionPath = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
                Directory.CreateDirectory(_sessionPath);
            }
            else
                _sessionPath = "";

            _mode = mode;

            GridSize = 300;
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

        unsafe void SaveTextureRaw(Texture2D texture, TextureFormat format, int bytesPerPixel, string path)
        {
            _buffers ??= [MemoryBuffer.Create<byte>(16), MemoryBuffer.Create<byte>(16)];

            OpenGLRender.Current!.ReadTexture(texture, format, 0, 0, _buffers);

            var size = checked((int)(texture.Width * texture.Height * bytesPerPixel));

            using var pTex = _buffers[0].MemoryLock();
            using var file = File.Create(path);

            file.Write(new ReadOnlySpan<byte>(pTex.Data, size));
        }

        Texture2D LoadColorTextureRaw(string path, int width, int height, string name)
        {
            var data = File.ReadAllBytes(path);

            var texture = CreateTexture((uint)width, (uint)height);

            texture.LoadData(new TextureData
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = TextureFormat.Rgba32,
                Data = MemoryBuffer.Create(data)
            });

            return texture;
        }

        void SaveFrame(DepthFrame frame, DepthFrameMeta meta)
        {
            var framePath = Path.Combine(_sessionPath, $"frame_{meta.Frame:000000}");
            Directory.CreateDirectory(framePath);

            var jsonPath = Path.Combine(framePath, "meta.json");
            var depthPath = Path.Combine(framePath, "depth_u16.raw");
            var colorPath = Path.Combine(framePath, "color_rgba.raw");

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(meta, _jsonOptions));

            SaveTextureRaw(
                frame.CameraTexture!,
                TextureFormat.Rgba32,
                4,
                colorPath);

            var mat = (EnvDepthMaterial)_envDepth!.Materials[0];

            SaveTextureRaw(
                mat.LastTexture!,
                TextureFormat.GrayInt16,
                2,
                depthPath);
        }

        protected Material CreateMaterial(Texture2D texture)
        {
            if (_mode == DepthSnapeshotMode.Read)
            {
                return new GridMaterial()
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
                UseDepth = true,
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
                Format = TextureFormat.Rgba32,
            };
        }

        public unsafe void Load(
            string path,
            bool clear = false,
            float cleanupMargin = -0.01f)
        {
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
                {
                    var write = 0;

                    for (var read = 0; read < splats.Count; read++)
                    {
                        var splat = splats[read];

                        var clip = Vector4.Transform(
                            new Vector4(splat.Position, 1.0f),
                            colorViewProj
                        );

                        var remove = false;

                        if (clip.W > 0.00001f)
                        {
                            var invW = 1.0f / clip.W;

                            var x = clip.X * invW;
                            var y = clip.Y * invW;
                            var z = clip.Z * invW;

                            remove =
                                x >= -1.0f - cleanupMargin && x <= 1.0f + cleanupMargin &&
                                y >= -1.0f - cleanupMargin && y <= 1.0f + cleanupMargin &&
                                z >= -1.0f - cleanupMargin && z <= 1.0f + cleanupMargin;
                        }

                        if (!remove)
                            splats[write++] = splat;
                    }

                    if (write < splats.Count)
                        splats.RemoveRange(write, splats.Count - write);
                }

                var depthBytes = File.ReadAllBytes(depthPath);

                Geometry3D geometry;

                fixed (byte* pBytes = depthBytes)
                {
                    geometry = EnvDepthMesh.CreateDepthColorGrid(
                        (ushort*)pBytes,
                        meta.DepthWidth,
                        meta.DepthHeight,
                        GridSize,
                        GridSize,
                        Matrix4x4.Invert(meta.DepthView * meta.DepthProj, out var inv)
                            ? inv
                            : Matrix4x4.Identity,
                        colorViewProj);
                }

                var mesh = new TriangleMesh(geometry);
                mesh.Name = $"Frame {meta.Frame}";
                mesh.SetProp("CameraView", meta.CameraView);
 
                var colorTexture = LoadColorTextureRaw(
                    colorPath,
                    meta.ColorWidth,
                    meta.ColorHeight,
                    $"Tex Frame {meta.Frame}");

                mesh.Materials.Add(CreateMaterial(colorTexture));
                mesh.Materials.Add(new WireframeMaterial()
                {
                    Color = new Color(1, 1, 1, 1),
                    IsEnabled = false
                });

                mesh.AddComponent(new CaptureFrame
                {
                    Meta = meta,
                    Texture = colorTexture
                });


                if (SplatMode)
                {
                    DepthGridSplatBuilder.CreateSplats(
                        splats,
                        geometry,
                        colorTexture.Data![0].Data!,
                        (int)colorTexture.Width,
                        (int)colorTexture.Height);
                }

                var frame = new DepthFrame
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
                };

                _frames.Add(frame);

                if (!SplatMode)
                    _host!.AddChild(mesh);
            }

            _frameIndex = _frames.Count == 0
                ? 0
                : _frames.Count;

            if (SplatMode)
            {
                _splatMesh = new SplatMesh(splats.ToArray());
                _host!.AddChild(_splatMesh);
            }
        }

        public DepthFrame? CreateSnapeshot()
        {
            var camera = _capture!.GetCameraStatus(OculusCameras.Left);

            if (!camera.IsActive)
                return null;

            var cameraTime = camera.FrameTime;

            var cameraWorld = camera.Pose?.ToMatrix() ?? Matrix4x4.Identity;

            Matrix4x4.Invert(cameraWorld, out var cameraView);

            var cameraViewProj = cameraView * camera.Proj!.Value;

            var frozenMesh = _envDepth!.Freeze(cameraViewProj);

            if (frozenMesh == null)
                return null;

            var mat = (EnvDepthMaterial)_envDepth.Materials[0];

            var frameTexture = CreateTexture(camera.Texture!.Width, camera.Texture.Height);

            GlImageProc.CopyColor(camera.Texture!.ToGlTexture(), frameTexture.ToGlTexture());

            frozenMesh.Materials.Add(CreateMaterial(frameTexture));
          
            var frame = new DepthFrame
            {
                CameraProj = camera.Proj!.Value,
                CameraView = cameraView,
                CameraXrTime = cameraTime,
                Mesh = frozenMesh,
                DepthView = mat.DepthCamera.Eyes![0].View,
                DepthProj = mat.DepthCamera.Eyes![0].Projection,
                DepthXrTime = mat.LastFrameTime,
                FrameXrTime = XrApp.Current!.FramePredictedDisplayTime,
                CameraTexture = frameTexture
            };

            if (_mode == DepthSnapeshotMode.Record)
            {
                var size = ((Grid3D)_envDepth.Geometry!).Size;

                var meta = new DepthFrameMeta
                {
                    Frame = _frameIndex++,

                    ColorWidth = (int)camera.Texture!.Width,
                    ColorHeight = (int)camera.Texture.Height,

                    DepthWidth = (int)mat.LastTexture!.Width,
                    DepthHeight = (int)mat.LastTexture.Height,

                    GridWidth = (int)size.Width,
                    GridHeight = (int)size.Height,

                    CameraProj = camera.Proj!.Value,
                    CameraView = cameraView,

                    DepthView = mat.DepthCamera.Eyes![0].View,
                    DepthProj = mat.DepthCamera.Eyes![0].Projection,

                    CameraXrTime = cameraTime,
                    DepthXrTime = mat.LastFrameTime,
                    FrameXrTime = XrApp.Current!.FramePredictedDisplayTime
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


        protected void UpdateAlphaDistance(RenderContext ctx)
        {
            var currentCameraPos = ctx.Camera!.WorldPosition;
            var currentViewInv = ctx.Camera.ViewProjectionInverse;
            var currentForward = ctx.Camera.Forward;

            foreach (var child in _host!.Children)
            {
                if (child is not TriangleMesh mesh)
                    continue;

                var captureView = mesh.GetProp<Matrix4x4>("CameraView");

                if (mesh.Materials.Count == 0 || mesh.Materials[0] is not TextureMaterial mat)
                    continue;

                var captureViewInv = captureView.Invert();

                var captureCameraPos = captureViewInv.Translation;

                var captureForward = -Vector3.Normalize(new Vector3(
                    captureViewInv.M31,
                    captureViewInv.M32,
                    captureViewInv.M33
                ));

                var d = Vector3.Distance(currentCameraPos, captureCameraPos);

                var distanceT = Math.Clamp(
                    (d - FullAlphaDistance) / (ZeroAlphaDistance - FullAlphaDistance),
                    0.0f,
                    1.0f
                );

                var distanceWeight = 1.0f - distanceT;

                var orientationDot = Vector3.Dot(currentForward, captureForward);

                var orientationT = Math.Clamp(
                    (orientationDot - ZeroAlphaOrientationDot) /
                    (FullAlphaOrientationDot - ZeroAlphaOrientationDot),
                    0.0f,
                    1.0f
                );

                var weight = distanceWeight * orientationT;

                var alpha = MinAlpha + (MaxAlpha - MinAlpha) * weight;

                mat.Color = new Color(1, 1, 1, alpha);
            }
        }

        protected override void Update(RenderContext ctx)
        {
            if (_mode == DepthSnapeshotMode.Read)
            {
                //UpdateAlphaDistance(ctx);
                return;
            }

            Debug.Assert(_captureBtn != null && _deleteBtn != null);

            if (_captureBtn.IsChanged && _captureBtn!.Value)
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

        public int GridSize { get; set; }

        public bool SplatMode { get; set; }

        public bool Clip { get; set; }


        [Range(0, 10f, 0.1f)]
        public float FullAlphaDistance { get; set; } = 0.20f;

        [Range(0, 10f, 0.1f)]
        public float ZeroAlphaDistance { get; set; } = 1.25f;


        [Range(0, 1f, 0.01f)]
        public float MinAlpha { get; set; } = 0.05f;

        [Range(0, 1f, 0.01f)]
        public float MaxAlpha { get; set; } = 0.90f;


        [Range(0, 1f, 0.01f)]
        public float ZeroAlphaOrientationDot { get; set; } = 0.30f;

        [Range(0, 1f, 0.01f)]
        public float FullAlphaOrientationDot { get; set; } = 0.95f;
    }
}

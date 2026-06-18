using OpenXr.Framework;

using System.Diagnostics;
using System.Numerics;

using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples.Components
{
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


        CameraController? _capture;
        EnvDepthMesh? _envDepth;
        XrBoolInput? _captureBtn;
        XrBoolInput? _deleteBtn;

        readonly List<DepthFrame> _frames = [];

        protected override void Start(RenderContext ctx)
        {
            Debug.Assert(_host?.Scene != null);

            if (!_host.Scene.TryComponent(out _capture))
            {
                _capture = new CameraController();
                _host.Scene.AddComponent(_capture); 
            }

            _envDepth = _host.Scene.Descendants<EnvDepthMesh>().FirstOrDefault();

            _envDepth ??= _host.AddChild(new EnvDepthMesh(new Size2I(300, 300)));

            _ = _capture.StartCameraAsync(OculusCameras.Left);

            base.Start(ctx);
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

            var frameTexture = new Texture2D
            {
                Width = camera.Texture!.Width,
                Height = camera.Texture.Height,
                MipLevelCount = 0,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Format = TextureFormat.Rgba32,
                Name = $"Frame {camera.Frame}"
            };

            GlImageProc.CopyColor(camera.Texture!.ToGlTexture(), frameTexture.ToGlTexture());

            frozenMesh.Materials.Add(new TextureMaterial(frameTexture));
            
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
            if (_captureBtn!.IsChanged && _captureBtn!.Value)
            {
                var frame = CreateSnapeshot();

                if (frame != null)
                {
                    _frames.Add(frame);

                    _host!.AddChild(frame.Mesh!);
                }
            }

            if (_deleteBtn!.IsChanged && _deleteBtn.Value)
                DeleteLast();

            base.Update(ctx);
        }

        public void ConfigureInput(IXrBasicInteractionProfile input)
        {
            _captureBtn = input.Right!.Button!.AClick!;
            _deleteBtn = input.Right!.Button!.BClick!;
        }
    }
}

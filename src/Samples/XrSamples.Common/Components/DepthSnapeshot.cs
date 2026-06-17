using OpenXr.Framework;

using System.Diagnostics;
using System.Numerics;

using XrEngine;
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

            public Pose3? HeadPose;

            public Matrix4x4 DepthView;

            public Matrix4x4 DepthProj;

            public Matrix4x4 CameraView;

            public Matrix4x4 CameraProj;

            public long DepthXrTime;

            public long CameraXrTime;

            public long FrameXrTime;    
        }


        CameraCapture? _capture;
        EnvDepthMesh? _envDepth;
        XrBoolInput? _captureBtn;
        XrBoolInput? _deleteBtn;

        readonly List<DepthFrame> _frames = [];

        protected override void Start(RenderContext ctx)
        {
            Debug.Assert(_host?.Scene != null);

            if (!_host.Scene.TryComponent(out _capture))
            {
                _capture = new CameraCapture();
                _host.Scene.AddComponent(_capture); 
            }

            _envDepth = _host.Scene.Descendants<EnvDepthMesh>().FirstOrDefault();

            _envDepth ??= _host.AddChild(new EnvDepthMesh(new Size2I(300, 300)));
        

            base.Start(ctx);
        }

        public DepthFrame? CreateSnapeshot()
        {
            if (_capture?.Camera == null || _capture.Camera.LastFrame == 0)
                return null;

            var cameraTime = _capture.Camera.LastTimestamp;

            var headPose = XrApp.Current!.LocateSpace(XrApp.Current!.Head, XrApp.Current.ReferenceSpace, cameraTime).Pose;

            var camParams = _capture.Camera.GetParams();

            var cameraWorld = camParams.GetLensPose().ToMatrix() * headPose.ToMatrix();

            Matrix4x4.Invert(cameraWorld, out var cameraView);

            var cameraProj = camParams.CreateProjection(0.05f, 20.0f);

            var cameraViewProj = cameraView * cameraProj;

            var frozenMesh = _envDepth!.Freeze(cameraViewProj);

            if (frozenMesh == null)
                return null;

            var mat = (EnvDepthMaterial)_envDepth.Materials[0];

            var frameTexture = new Texture2D
            {
                Width = _capture.Texture.Width,
                Height = _capture.Texture.Height,
                MipLevelCount = 0,
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Format = TextureFormat.Rgba32,
                Name = $"Frame {_capture.Camera.LastFrame}"
            };

            GlImageProc.CopyColor(_capture.Texture.ToGlTexture(), frameTexture.ToGlTexture());

            frozenMesh.Materials.Add(new TextureMaterial(frameTexture));
            
            var frame = new DepthFrame
            {
                CameraProj = cameraProj,
                CameraView = cameraView,
                CameraXrTime = cameraTime,
                Mesh = frozenMesh,
                HeadPose = headPose,
                DepthView = mat.DepthCamera.Eyes![0].View,
                DepthProj = mat.DepthCamera.Eyes![0].Projection,
                DepthXrTime = mat.LastFrameTime,
                FrameXrTime = XrApp.Current.FramePredictedDisplayTime,
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

            //lastFrame.CameraTexture!.Dispose();
            //lastFrame.Mesh!.Dispose();

            lastFrame.Mesh!.Remove();
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

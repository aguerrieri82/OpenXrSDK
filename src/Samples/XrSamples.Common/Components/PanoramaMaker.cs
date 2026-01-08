using OpenXr.Framework;
using System.Numerics;
using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenGL;
using XrMath;
using System.Diagnostics;



#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrSamples
{
    public class PanoramaMaker : Behavior<Scene3D>
    {
        ICameraDevice? _camera;
        private CameraParams? _cameraParams;
        private int _cameraState;
        private Pose3 _lastCameraPose;
        private XrBoolInput? _trigger;
        private readonly TextureCube _cubeTexture;
        private GlComputeProgram? _snapProgram;
        private Pose3? _rootPose;
        private readonly Texture2D _cameraTexture;

        public PanoramaMaker()
        {
            _cubeTexture = new TextureCube();
            _cubeTexture.LoadData(new TextureData
            {
                Width = 1024,
                Height = 1024,
                Format = TextureFormat.RgbaFloat16
            });

            _cameraTexture = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };
        }

        public void Configure(IXrBasicInteractionProfile inputs)
        {
            _trigger = inputs.Right!.TriggerClick;
        }

        protected override void Start(RenderContext ctx)
        {
            _cameraTexture.ToGlTexture();

            var manager = Context.Require<ICameraManager>();
            var cameras = manager.GetCameras();

            _ = Task.Run(async () =>
            {
                var infoLeft = cameras.First(a => a.Source == 0 && a.Position == 0);

                _camera = await manager.OpenCameraAsync(infoLeft.Id!);

                _cameraParams = _camera.GetParams();

                var formats = _camera.GetSupportedFormats();

                var curFormat = formats.Last();

                await _camera.StartCaptureAsync(curFormat, _cameraTexture);

                _cameraState = 1;

            });

            _snapProgram = new GlComputeProgram(OpenGLRender.Current!.GL, "cube_project.comp", str => Embedded.GetString<PanoramaMaker>(str));
            _snapProgram.AddExtension("GL_OES_EGL_image_external_essl3");
            _snapProgram.Build();

            base.Start(ctx);
        }

        protected void TakeFrame()
        {
            Debug.Assert(_cubeTexture != null);
            Debug.Assert(_cameraParams != null);
            Debug.Assert(_snapProgram != null);

            var renderer = OpenGLRender.Current!;
            var gl = renderer.GL;

            _snapProgram.Use();
            _snapProgram.SetUniform("uSourceTexture", _cameraTexture, 0);

            Matrix4x4.Invert(_cameraParams.GetLensPose().ToMatrix() * _lastCameraPose.ToMatrix(), out var viewMatrix);

            _snapProgram.SetUniform("uViewInv", viewMatrix);
            _snapProgram.SetUniform("uFaceSize", (float)_cubeTexture.Width);
            _snapProgram.SetUniform("uProxyDepth", (float)3);
            _snapProgram.SetUniform("uK", new Matrix3x3(
                _cameraParams.Fx, 0, 0,
                0, _cameraParams.Fy, 0,
                _cameraParams.Cx, _cameraParams.Cy, 1));

            var format = GlUtils.GetInternalFormat(_cubeTexture.Format, TextureCompressionFormat.Uncompressed);

            gl.BindImageTexture(0, (uint)_cubeTexture.Handle, 0, true, 0, BufferAccessARB.ReadOnly, (GLEnum)format);
            gl.BindImageTexture(1, (uint)_cubeTexture.Handle, 0, true, 0, BufferAccessARB.WriteOnly, (GLEnum)format);

            gl.DispatchCompute(_cubeTexture.Width / 16, _cubeTexture.Height / 16, 6);

            gl.MemoryBarrier(MemoryBarrierMask.ShaderStorageBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
        }

        protected override void Update(RenderContext ctx)
        {

            if (_cameraState != 1 || XrApp.Current == null || _camera == null)
                return;

            _camera.UpdateTexture();

            _lastCameraPose = XrApp.Current!.LocateSpace(XrApp.Current.Head,
                XrApp.Current.ReferenceSpace, _camera.LastTimestamp).Pose;

            if (_rootPose == null)
                _rootPose = _lastCameraPose;

            _lastCameraPose.Position -= _rootPose.Value.Position;

            _lastCameraPose.Position = Vector3.Zero;

            if (_trigger != null && _trigger.Value)
                TakeFrame();

            base.Update(ctx);
        }


        public TextureCube CubeTexture => _cubeTexture;

        public Texture2D CameraTexture => _cameraTexture;
    }
}

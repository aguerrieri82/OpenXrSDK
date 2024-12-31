using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;


namespace XrEngine.OpenXr
{
    public class OculusEnvDepthProvider : BaseComponent<Camera>, IEnvDepthProvider
    {
        protected readonly XrApp _xrApp;
        protected readonly Dictionary<long, Texture2D> _textures;
        protected readonly XrPassthroughLayer _passTh;
        protected long _lastFrameTime;
        protected Texture2D? _outTexture;
        protected Texture2D? _lastTexture;
        protected Camera? _lastCamera;

        public OculusEnvDepthProvider(XrApp xrApp)
        {
            _passTh = xrApp.Layers.List.OfType<XrPassthroughLayer>().Single();
            _passTh.UseEnvironmentDepth = true;
            _xrApp = xrApp;
            _textures = [];
            _lastFrameTime = -1;
            Blur = true;
        }


        public unsafe Texture2D? Acquire(Camera depthCamera)
        {
            if (_passTh.DepthImage == null)
                return null;

            if (_xrApp.FramePredictedDisplayTime == _lastFrameTime)
            {
                depthCamera.Far = _lastCamera!.Far;
                depthCamera.Near = _lastCamera.Near;
                depthCamera.Eyes = _lastCamera.Eyes;
                depthCamera.Projection = _lastCamera.Projection;
                depthCamera.View = _lastCamera.View;
                return _lastTexture;
            }


            depthCamera.Eyes ??= new CameraEye[2];

            if (depthCamera.ActiveEye == 0)
            {
                var data = _passTh.DepthImage.Value;

                depthCamera.Far = float.IsInfinity(data.FarZ) ? 0 : data.FarZ;
                depthCamera.Near = data.NearZ;

                for (var i = 0; i < 2; i++)
                {
                    var view = data.Views[i];
                    var transform = XrCameraTransform.FromView(view.Pose.ToPose3(), view.Fov, depthCamera.Near, depthCamera.Far);

                    var cameraView = new Matrix4x4();

                    Matrix4x4.Invert(transform.Transform, out cameraView);

                    depthCamera.Eyes[i] = new CameraEye
                    {
                        Projection = transform.Projection,
                        World = transform.Transform,
                        View = cameraView,
                        ViewProj = cameraView * transform.Projection,
                    };
                }

                var img = _passTh.EnvironmentDepth.Images!.ItemPointer((int)data.SwapchainIndex);
                var type = img->Type;

                if (type == StructureType.SwapchainImageOpenglKhr ||
                    type == StructureType.SwapchainImageOpenglESKhr)
                {
                    var glImg = *(SwapchainImageOpenGLKHR*)img;

                    if (!_textures.TryGetValue(glImg.Image, out var texture))
                    {
                        texture = new Texture2D
                        {
                            Handle = glImg.Image
                        };
                        _textures[glImg.Image] = texture;
                    }

                    if (Blur)
                    {
                        if (_host?.Scene?.App?.Renderer is ITextureFilterProvider filter)
                        {
                            _outTexture ??= new Texture2D()
                            {
                                Width = (uint)_passTh.EnvironmentDepth.Size.Width,
                                Height = (uint)_passTh.EnvironmentDepth.Size.Height,
                                Format = TextureFormat.GrayInt16,
                                MinFilter = ScaleFilter.Linear,
                                MagFilter = ScaleFilter.Linear,
                                MipLevelCount = 1,
                                Depth = 2,
                                WrapS = WrapMode.ClampToEdge,
                                WrapT = WrapMode.ClampToEdge,
                            };
                            filter.Blur(texture, _outTexture);
                            _lastTexture = _outTexture;
                        }
                    }
                    else
                        _lastTexture = texture;
                }
            }

            depthCamera.Projection = depthCamera.Eyes[depthCamera.ActiveEye].Projection;
            depthCamera.WorldMatrix = depthCamera.Eyes[depthCamera.ActiveEye].World;

            _lastFrameTime = _xrApp.FramePredictedDisplayTime;

            _lastCamera = depthCamera;

            return _lastTexture;
        }

        [Range(-1, 1, 0.001f)]
        public float Bias { get; set; }

        public bool Blur { get; set; }

        public bool RemoveHand
        {
            get => _passTh.RemoveHand;
            set => _passTh.RemoveHand = value;
        }
    }
}

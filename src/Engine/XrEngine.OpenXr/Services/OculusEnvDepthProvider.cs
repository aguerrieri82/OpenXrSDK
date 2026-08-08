#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using StructureType = Silk.NET.OpenXR.StructureType;
using Silk.NET.Vulkan;
using XrMath;

namespace XrEngine.OpenXr
{
    public class OculusEnvDepthProvider : BaseComponent<Camera>, IEnvDepthProvider
    {
        protected readonly XrApp _xrApp;
        protected readonly Dictionary<long, Texture2D> _textures;
        protected readonly XrPassthroughLayer _passTh;
        protected long _lastFrameTime;
        protected bool _useAngle;
        protected Texture2D? _outTexture;
        protected Texture2D? _lastTexture;
        protected Camera? _lastCamera;
        protected uint _lastGlImage;

        public OculusEnvDepthProvider(XrApp xrApp)
        {
            _passTh = xrApp.Layers.List.OfType<XrPassthroughLayer>().Single();
            _passTh.UseEnvironmentDepth = true;
            _xrApp = xrApp;
            _textures = [];
            _lastFrameTime = -1;
            _useAngle = _xrApp.Plugin<IXrGraphicDriver>() is XrAngleGraphicDriver;
            Blur = true;
        }

        public Texture2D? Acquire(Camera depthCamera, out long frameTime)
        {
            var result = Acquire(depthCamera);
            frameTime = _lastFrameTime;
            return result;
        }

        public unsafe Texture2D? Acquire(Camera depthCamera)
        {
            if (_passTh.DepthImage == null)
                return null;

            if (!_passTh.IsStarted)
                return null;

            if (_xrApp.FramePredictedDisplayTime == _lastFrameTime || Freeze)
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

                    var cameraView = transform.World.Invert();

                    depthCamera.Eyes[i] = new CameraEye
                    {
                        Projection = transform.Projection,
                        World = transform.World,
                        View = cameraView,
                        ViewProj = cameraView * transform.Projection,
                    };

                    depthCamera.Eyes[i].ViewProjInv = depthCamera.Eyes[i].ViewProj.Invert();
                }

                var image = _passTh.EnvironmentDepth.Images!.ItemPointer((int)data.SwapchainIndex);
                var type = image->Type;

                if (type == StructureType.SwapchainImageOpenglKhr ||
                    type == StructureType.SwapchainImageOpenglESKhr || _useAngle)
                {

                    if (_useAngle)
                    {
                        var ctx = Context.Require<AngleVulkanContext>();
                        var vkImage = (nint)((SwapchainImageVulkanKHR*)image)->Image;

                        _lastGlImage = ctx.AttachVulkanImage(
                            vkImage,
                            Format.D16Unorm,
                            _passTh.EnvironmentDepth.Size,
                            2, 1, 1,
                            ImageUsageFlags.SampledBit |
                            ImageUsageFlags.DepthStencilAttachmentBit,
                            ImageCreateFlags.None,
                            TextureTarget.Texture2DArray).Texture;

                        ctx.AcquireTexture(_lastGlImage);
                    }
                    else
                        _lastGlImage = ((SwapchainImageOpenGLKHR*)image)->Image;

                    if (!_textures.TryGetValue(_lastGlImage, out var texture))
                    {
                        texture = new Texture2D
                        {
                            Handle = _lastGlImage
                        };
                        _textures[_lastGlImage] = texture;
                    }

                    if (Blur)
                    {
                        var filter = _host?.Scene?.App?.Renderer.Feature<ITextureFilterProvider>();
                        if (filter != null)
                        {
                            _outTexture ??= new Texture2D()
                            {
                                Width = (uint)_passTh.EnvironmentDepth.Size.Width,
                                Height = (uint)_passTh.EnvironmentDepth.Size.Height,
                                Format = _useAngle ? TextureFormat.RgbaFloat16 : TextureFormat.Gray16,
                                MinFilter = ScaleFilter.Linear,
                                MagFilter = ScaleFilter.Linear,
                                MipLevelCount = 1,
                                Depth = 2,
                                WrapS = WrapMode.ClampToEdge,
                                WrapT = WrapMode.ClampToEdge,
                            };
                            filter.Blur(texture, _outTexture, "Depth_Blur", 1, 0);
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

        public bool Freeze { get; set; }

        public bool RemoveHand
        {
            get => _passTh.RemoveHand;
            set => _passTh.RemoveHand = value;
        }
    }
}

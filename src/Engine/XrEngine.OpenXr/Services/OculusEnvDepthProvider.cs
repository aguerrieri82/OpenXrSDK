using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;


namespace XrEngine.OpenXr
{
    public class OculusEnvDepthProvider : BaseComponent<Camera>, IEnvDepthProvider
    {
        readonly XrApp _xrApp;
        readonly Dictionary<long, Texture2D > _textures;
        private Texture2D? _lastTexture;
        private XrPassthroughLayer _passTh;
        private long _lastFrameTime;

        public OculusEnvDepthProvider(XrApp xrApp)
        {
            _passTh = xrApp.Layers.List.OfType<XrPassthroughLayer>().Single();
            _passTh.UseEnvironmentDepth = true;
            _xrApp = xrApp;
            _textures = [];
        }


        public unsafe Texture2D? Acquire(Camera depthCamera)
        {
            if (_passTh.DepthImage == null)
                return null;

            if (_xrApp.FramePredictedDisplayTime == _lastFrameTime)
                return _lastTexture;

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

                    _lastTexture = texture;
                }

            }

            depthCamera.Projection = depthCamera.Eyes[depthCamera.ActiveEye].Projection;
            depthCamera.WorldMatrix = depthCamera.Eyes[depthCamera.ActiveEye].World;

            _lastFrameTime = _xrApp.FramePredictedDisplayTime;

            return _lastTexture;
        }

        public float Bias { get; set; }
    }
}

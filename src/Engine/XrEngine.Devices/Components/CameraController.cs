using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using XrEngine.Media;
using XrMath;

namespace XrEngine.Devices
{
    public class CameraStatus
    {
        public string? Id;

        public Texture2D? Texture;

        public Pose3? Pose;

        public long FrameTime;

        public long Frame;

        public bool IsActive;

        public CameraParams? Params;

        public ICameraDevice? Device;

        public Matrix4x4? Proj;

        public bool CaptureRequest;

        public VideoFormat Format;
    }

    public class CameraController : AsyncBehavior<Scene3D>, IDisposable
    {
        private ICameraManager? _manager;
        private ICameraPoseProvider? _poseProvider;
        private readonly Dictionary<string, CameraStatus> _cameras  = [];

        public CameraController()
        {
        }

        public CameraController(ICameraManager? manager)
        {
            _manager = manager;
        }

        protected override void OnAttach()
        {

            _manager ??= Context.Require<ILocalCameraManger>();

            if (_poseProvider == null)
                Context.TryRequire(out _poseProvider);

            base.OnAttach();
        }

        protected CameraStatus GetStatus(string cameraId)
        {
            if (!_cameras!.TryGetValue(cameraId, out var status))
            {
                status = new CameraStatus()
                {
                    Id = cameraId
                };
                _cameras[cameraId] = status;
            }
            return status;
        }

        public async Task<bool> StartCameraAsync(string cameraId, Size2? resolution = null, float? fps = null)
        {
            Debug.Assert(_manager != null);

            var status = GetStatus(cameraId);  
            
            if (status.Device == null)
            {
                var cameras = _manager.GetCameras();
                var info = cameras.Where(a => a.Id == cameraId);
                if (info == null)
                    return false;

                status.Device = await _manager.OpenCameraAsync(cameraId);
            }

            IEnumerable<VideoFormat> formats = status.Device.GetSupportedFormats()
                .Where(a => a.ImageFormat != ImageFormat.MJPG);

            if (resolution != null)
                formats = formats.Where(a => a.Width == resolution.Value.Width && a.Height == resolution.Value.Height);

            if (fps != null)
                formats = formats.Where(a => a.FrameRate == fps);

            var format = formats.OrderByDescending(a => a.Width * a.Height)
                .ThenByDescending(a => a.FrameRate)
                .FirstOrDefault();

            if (format.Width == 0)
                return false;

            status.Format = format;

            var formatChanged = status.Texture == null || status.Texture.Width != format.Width || status.Texture.Height != format.Height;

            if (formatChanged)
            {
                status.Texture?.Dispose();

                if (GetTexture != null)
                    status.Texture = GetTexture(cameraId);
                else
                {
                    status.Texture = new Texture2D()
                    {
                        Format = TextureFormat.Rgba32,
                        WrapT = WrapMode.ClampToEdge,
                        WrapS = WrapMode.ClampToEdge,
                        MagFilter = ScaleFilter.Linear,
                        MinFilter = ScaleFilter.Linear,
                        Type = TextureType.External,
                        Width = (uint)format.Width,
                        Height = (uint)format.Height
                    };
                }

                status.Texture.Generate();

            }

            if (status.Device.IsCapturing && formatChanged)
                status.Device.StopCapture();

            await status.Device.StartCaptureAsync(format, status.Texture);

            status.Params = status.Device.GetParams();

            status.IsActive = true;

            return true;
        }
      

        public void StopCamera(string cameraId)
        {
            var status = GetStatus(cameraId);

            if (status.Device != null)
            {
                if (status.Device.IsCapturing)
                    status.Device.StopCapture();

                if (status.Device.IsOpen)
                    status.Device.Close();
            }

            status.IsActive = false;
        }

        public CameraStatus GetCameraStatus(string cameraId)
        {
            var status = GetStatus(cameraId);
            return status;
        }

        protected override async Task UpdateAsync(RenderContext ctx)
        {
            foreach (var camera in _cameras.Values)
            {
                if (camera.IsActive && camera.Device != null)
                {
                    camera.Device.UpdateTexture();

                    if (camera.Device.LastTimestamp == camera.FrameTime)
                        continue;

                    camera.FrameTime = camera.Device.LastTimestamp;
                    camera.Frame = camera.Device.LastFrame;

                    if (_poseProvider != null && camera.FrameTime != 0)
                    {
                        var pose = _poseProvider.GetCameraPose(camera.Id!, camera.FrameTime);

                        if (pose != null)
                            camera.Pose = pose.Value.Multiply(camera.Params!.GetLensPose());
                        else
                            camera.Pose = null; 
                    }

                    if (camera.Proj == null)
                        camera.Proj = camera.Params!.GetProjection(0.1f, 10f);
                }
            }
        }

        public void Dispose()
        {
            foreach (var camera in _cameras.Values)
            {
                if (camera.Device is IDisposable disp)
                    disp.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public ICameraManager? Manger => _manager;

        public Func<string, Texture2D>? GetTexture { get; set; }
    }
}

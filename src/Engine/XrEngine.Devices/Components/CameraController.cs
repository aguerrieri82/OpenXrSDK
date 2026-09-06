using System.Diagnostics;
using System.Numerics;
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
        readonly SemaphoreSlim _cameraStartLock = new(1, 1);
        readonly Dictionary<string, Func<Task>> _cameraStartRequest = [];
        private ICameraManager? _manager;
        private ICameraPoseProvider? _poseProvider;
        private readonly Dictionary<string, CameraStatus> _cameras = [];

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
            if (!_cameras.TryGetValue(cameraId, out var status))
            {
                status = new CameraStatus()
                {
                    Id = cameraId
                };
                _cameras[cameraId] = status;
            }
            return status;
        }

        static Rect2 CalcSensorCropRegion(
            float sensorWidth,
            float sensorHeight,
            float currentWidth,
            float currentHeight)
        {
            var scaleX = currentWidth / sensorWidth;
            var scaleY = currentHeight / sensorHeight;

            var maxScale = MathF.Max(scaleX, scaleY);

            scaleX /= maxScale;
            scaleY /= maxScale;

            return new Rect2
            {
                X = sensorWidth * (1.0f - scaleX) * 0.5f,
                Y = sensorHeight * (1.0f - scaleY) * 0.5f,
                Width = sensorWidth * scaleX,
                Height = sensorHeight * scaleY
            };
        }

        public Matrix3x3 GetUvTransform(string cameraId, Matrix4x4 eyeView, Matrix4x4 eyeProjection)
        {
            static Matrix3x3 QuadToUv(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
            {
                var dx1 = p1.X - p2.X;
                var dx2 = p3.X - p2.X;
                var dx3 = p0.X - p1.X + p2.X - p3.X;
                var dy1 = p1.Y - p2.Y;
                var dy2 = p3.Y - p2.Y;
                var dy3 = p0.Y - p1.Y + p2.Y - p3.Y;

                var d = dx1 * dy2 - dx2 * dy1;
                var g = (dx3 * dy2 - dx2 * dy3) / d;
                var h = (dx1 * dy3 - dx3 * dy1) / d;

                var m = new Matrix3x3(
                    p1.X - p0.X + g * p1.X, p3.X - p0.X + h * p3.X, p0.X,
                    p1.Y - p0.Y + g * p1.Y, p3.Y - p0.Y + h * p3.Y, p0.Y,
                    g, h, 1);

                return m.Invert();
            }

            var status = GetStatus(cameraId);
            var cam = status.Params!;

            if (status.Pose == null)
                return Matrix3x3.Identity;

            const float distance = 3f;

            var crop = CalcSensorCropRegion(
                cam.SensorSize!.Value.Width,
                cam.SensorSize.Value.Height,
                cam.CurrentSize.Width,
                cam.CurrentSize.Height);

            var x0 = distance * ((crop.X - cam.Cx) / cam.Fx);
            var x1 = distance * ((crop.X + crop.Width - cam.Cx) / cam.Fx);
            var y0 = distance * ((crop.Y - cam.Cy) / cam.Fy);
            var y1 = distance * ((crop.Y + crop.Height - cam.Cy) / cam.Fy);

            var transform = status.Pose.Value.ToMatrix() * eyeView * eyeProjection;

            Vector2 Project(float x, float y)
            {
                var p = Vector4.Transform(new Vector4(x, y, -distance, 1), transform);
                return new Vector2(
                    p.X / p.W * 0.5f + 0.5f,
                    p.Y / p.W * 0.5f + 0.5f);
            }

            var p0 = Project(x0, y1); // uv 0,0
            var p1 = Project(x1, y1); // uv 1,0
            var p2 = Project(x1, y0); // uv 1,1
            var p3 = Project(x0, y0); // uv 0,1

            return QuadToUv(p0, p1, p2, p3);
        }

        public void StartCamera(string cameraId, Size2? resolution = null, float? fps = null)
        {
            _cameraStartLock.Wait();
            try
            {
                _cameraStartRequest[cameraId] = () => StartCameraAsync(cameraId, resolution, fps);
            }
            finally
            {
                _cameraStartLock.Release();
            }
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

            var formats = status.Device.GetSupportedFormats()
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
                        Format = TextureFormat.Rgba8,
                        WrapT = WrapMode.ClampToEdge,
                        WrapS = WrapMode.ClampToEdge,
                        MagFilter = ScaleFilter.Linear,
                        MinFilter = ScaleFilter.Linear,
                        Type = TextureType.External,
                        Width = (uint)format.Width,
                        Height = (uint)format.Height,
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
            if (_cameraStartRequest.Count > 0)
            {
                await _cameraStartLock.WaitAsync();

                try
                {
                    foreach (var req in _cameraStartRequest)
                        await req.Value();

                    _cameraStartRequest.Clear();
                }
                finally
                {
                    _cameraStartLock.Release();
                }
            }

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

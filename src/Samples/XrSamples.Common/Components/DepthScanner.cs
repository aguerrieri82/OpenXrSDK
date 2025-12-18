
using Common.Interop;
using Newtonsoft.Json;
using OpenXr.Framework;
using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples
{
    public class DepthScanner : Behavior<Object3D>
    {
        readonly List<PointData> _points = [];
        double _lastScanTime;
        readonly PerspectiveCamera _depthCamera;
        string? _sessionName;

        struct CameraInfo
        {
            public Vector3 Position;

            public Quaternion Orientation;

            public Matrix4x4 View;

            public Matrix4x4 Proj;
        }

        public DepthScanner()
        {
            _depthCamera = new PerspectiveCamera();
            Color = "#ff0000";
        }

        protected override void Update(RenderContext ctx)
        {
            if (FrameRate > 0)
            {
                if ((ctx.Time - _lastScanTime) >= 1f / FrameRate)
                    Scan(ctx);
            }
            else
            {
                if (ScanInput != null && ScanInput.IsActive && ScanInput.IsChanged && ScanInput.Value)
                    Scan(ctx);
            }

            if (HideInput != null && HideInput.IsActive && HideInput.IsChanged && HideInput.Value)
                _host!.IsVisible = !_host.IsVisible;

            if (ClearInput != null && ClearInput.IsActive && ClearInput.IsChanged && ClearInput.Value)
            {
                _points.Clear();
                if (_host is PointMesh pm)
                {
                    pm.Vertices = [];
                    pm.NotifyChanged(ObjectChangeType.Geometry);
                }
            }

            base.Update(ctx);
        }

        unsafe void Scan(RenderContext ctx)
        {
            IEnvDepthProvider? depth = _host?.Scene?.ActiveCamera?.Feature<IEnvDepthProvider>();
            if (depth == null)
                return;

            Texture2D? texture = depth.Acquire(_depthCamera);
            if (texture == null)
                return;

            if (texture.MagFilter != ScaleFilter.Nearest)
            {
                texture.MagFilter = ScaleFilter.Nearest;
                texture.MinFilter = ScaleFilter.Nearest;
                texture.NotifyChanged(ObjectChangeType.Render);
                return;
            }

            TextureData texData = _host!.Scene!.App!.Renderer!.ReadTexture(texture, texture.Format)![0];

            using MemoryLock<byte> data = texData.Data!.MemoryLock();

            ushort* fData = (ushort*)data.Data;

            List<Vector3> curPoints = new List<Vector3>();

            for (int x = 2; x < texture.Width - 4; x++)
            {
                for (int y = 2; y < texture.Height - 4; y++)
                {
                    float z = fData[y * texture.Width + x] / (float)ushort.MaxValue;

                    Vector3 pos = new Vector3(
                           2.0f * x / texture.Width - 1.0f,
                           2.0f * y / texture.Height - 1.0f,
                           2f * z - 1f
                       );

                    Vector3 worldPos = _depthCamera.Unproject(pos);

                    curPoints.Add(worldPos);
                }
            }

            if (_host is PointMesh pm)
            {
                foreach (Vector3 point in curPoints)
                    _points.Add(new PointData
                    {
                        Color = Color,
                        Pos = point,
                        Size = 1
                    });

                pm.Vertices = _points.ToArray();
                pm.NotifyChanged(ObjectChangeType.Geometry);
            }

            if (!string.IsNullOrWhiteSpace(SavePath))
            {
                _sessionName ??= $"Scan_{DateTime.Now.Ticks}";

                string baseDir = Path.Join(SavePath, _sessionName);

                Directory.CreateDirectory(baseDir);

                CameraEye eye = _depthCamera.Eyes![0];
                Matrix4x4.Decompose(eye.World, out Vector3 scale, out Quaternion rotation, out Vector3 translation);

                CameraInfo camera = new CameraInfo
                {
                    Orientation = rotation,
                    Position = translation,
                    Proj = eye.Projection,
                    View = eye.View,
                };

                Vector3[] array = curPoints.ToArray();

                fixed (Vector3* pPoints = array)
                {
                    Span<byte> span = new Span<byte>(pPoints, sizeof(Vector3) * array.Length);
                    File.WriteAllBytes(Path.Join(baseDir, $"{ctx.Frame}-points.bin"), span);
                }

                string json = JsonConvert.SerializeObject(camera);

                File.WriteAllText(Path.Join(baseDir, $"{ctx.Frame}-camera.json"), json);
            }

            _lastScanTime = ctx.Time;
        }

        public Color Color { get; set; }

        public string? SavePath { get; set; }

        public int FrameRate { get; set; }

        public XrInput<bool>? ScanInput { get; set; }

        public XrInput<bool>? ClearInput { get; set; }

        public XrInput<bool>? HideInput { get; set; }
    }
}

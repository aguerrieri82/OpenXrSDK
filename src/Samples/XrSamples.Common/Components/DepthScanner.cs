
using Newtonsoft.Json;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrMath;

namespace XrSamples.Components
{
    public class DepthScanner : Behavior<Object3D>
    {
        List<PointData> _points = [];
        double _lastScanTime;
        PerspectiveCamera _depthCamera;
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
            var depth = _host?.Scene?.ActiveCamera?.Feature<IEnvDepthProvider>();
            if (depth == null)
                return;

            var texture = depth.Acquire(_depthCamera);
            if (texture == null)
                return;

            if (texture.MagFilter != ScaleFilter.Nearest)
            {
                texture.MagFilter = ScaleFilter.Nearest;
                texture.MinFilter = ScaleFilter.Nearest;
                texture.NotifyChanged(ObjectChangeType.Render);
                return;
            }

            var texData = _host!.Scene!.App!.Renderer!.ReadTexture(texture, texture.Format)![0];

            using var data = texData.Data!.MemoryLock();

            var fData = (ushort*)data.Data;

            var curPoints = new List<Vector3>();

            for (var x = 2; x < texture.Width - 4; x++)
            {
                for (var y = 2; y < texture.Height - 4; y++)
                {
                    var z = fData[y * texture.Width + x] / (float)ushort.MaxValue;

                    var pos = new Vector3(
                           2.0f * x / texture.Width - 1.0f,
                           2.0f * y / texture.Height - 1.0f,
                           2f * z - 1f
                       );

                    var worldPos = _depthCamera.Unproject(pos);

                    curPoints.Add(worldPos);    
                }
            }

            if (_host is PointMesh pm)
            {
                foreach (var point in curPoints)
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

                var baseDir = Path.Join(SavePath, _sessionName);

                Directory.CreateDirectory(baseDir);

                var eye = _depthCamera.Eyes![0];
                Matrix4x4.Decompose(eye.World, out var scale, out var rotation, out var translation);

                var camera = new CameraInfo
                {
                    Orientation = rotation,
                    Position = translation,
                    Proj = eye.Projection,
                    View = eye.View,
                };

                var array = curPoints.ToArray();

                fixed (Vector3* pPoints = array)
                {
                    var span = new Span<byte>(pPoints, sizeof(Vector3) * array.Length);
                    File.WriteAllBytes(Path.Join(baseDir, $"{ctx.Frame}-points.bin"), span);
                }

                var json = JsonConvert.SerializeObject(camera);
                
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

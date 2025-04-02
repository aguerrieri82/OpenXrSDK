using System.Numerics;
using System.Text.Json;
using XrEngine;
using XrEngine.OpenGL;
using XrMath;

namespace XrSamples
{
    public class DepthMapDebug : Behavior<Scene3D>
    {
        int _leftFrames;
        bool _suspendRender;
        GlDepthPass? _depthPass;
        OpenGLRender? _renderer;
        GlLayerV2? _opaque;

        protected override void Start(RenderContext ctx)
        {
            ActiveObject = "#3396";
            _renderer = (OpenGLRender)ctx.Scene!.App!.Renderer!;
            _depthPass = _renderer.Pass<GlDepthPass>()!;
            _opaque = (GlLayerV2)_renderer.Layers.First(a => a.Type == GlLayerType.Opaque);

            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            lock (this)
            {
                while (_suspendRender && _leftFrames == 0)
                    Monitor.Wait(this);

                if (_leftFrames > 0)
                    _leftFrames--;
            }

            ActiveFrame = (uint)ctx.Frame;

            _depthPass!.UseDepthCull = UseDepthCull;

            base.Update(ctx);
        }


        [Action]
        public void PrintStatus()
        {
            _opaque = (GlLayerV2)_renderer!.Layers.First(a => a.Type == GlLayerType.Opaque);

            var draws = _opaque!.Content.Contents.Values
                .SelectMany(a => a.Contents.Values)
                .SelectMany(a => a.Contents.Values)
                .SelectMany(a => a.Contents);

            var draw = draws.FirstOrDefault(a => a.Object!.Name == ActiveObject);

            if (draw == null)
                Log.Warn(this, $"Draw '{ActiveObject}' not found");
            else
            {
                var bounds = draw.Object!.WorldBounds;
                var camera = _host!.Scene!.ActiveCamera!;

                var minUV = Vector2.One;
                var maxUV = Vector2.Zero;
                float minZ = 1.0f;

                var corners = bounds.Points.ToArray();

                for (int i = 0; i < 8; ++i)
                {
                    var clip = Vector4.Transform(new Vector4(corners[i], 1), camera.ViewProjection);
                    var ndc = clip / clip.W;
                    var norm = (ndc * 0.5f) + new Vector4(0.5f);
                    minUV = Vector2.Min(minUV, new Vector2(norm.X, norm.Y));
                    maxUV = Vector2.Max(maxUV, new Vector2(norm.X, norm.Y));
                    minZ = Math.Min(minZ, norm.Z);
                }

                var extent = (maxUV - minUV) * camera.ViewSize.ToVector2();

                var clipped = draws.Where(a => a.DepthData.IsCulled).Count();
                var hidden = draws.Where(a => !a.DepthData.IsVisible).Count();


                var data = JsonSerializer.Serialize(draw.DepthData, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
                Log.Info(this, "Draw Id: {0}\n{1}", draw.Id, data);

                Log.Info(this, "extent: {0}", extent);

                Log.Info(this, "hidden: {0}, clipped: {1}", hidden, clipped);
            }
        }


        [Action]
        public void RequestFrame()
        {
            if (!_suspendRender)
                return;

            lock (this)
            {
                _leftFrames++;
                Monitor.PulseAll(this);
            }

        }

        public string? ActiveObject { get; set; }

        public bool UseDepthCull { get; set; }

        public uint ActiveFrame { get; set; }

        public bool SuspendRender
        {
            get => _suspendRender;
            set
            {
                if (_suspendRender == value)
                    return;

                _suspendRender = value;

                lock (this)
                    Monitor.PulseAll(this);
            }
        }
    }
}

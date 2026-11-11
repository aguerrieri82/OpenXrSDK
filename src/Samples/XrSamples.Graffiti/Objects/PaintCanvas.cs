using SkiaSharp;
using System.Numerics;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;
using XrSamples.Graffiti.Objects;
using XrSamples.Graffiti.Shaders;

namespace XrSamples.Graffiti
{
    public enum PaintCanvasDebug
    {
        None = 0,
        Spray = 1,
        Wet = 3,
        Dry = 4
    }

    public class PaintCanvas : Group3D
    {
        protected Vector2 _size;
        protected readonly Texture2D _colorTexture;
        protected readonly Texture2D _normalTexture;
        protected readonly Texture2D _roughnessTexture;
        protected readonly Geometry3D _geo;
        protected readonly Texture2D _sprayTexture;
        protected float _texelSize;
        protected Can? _can;
        protected PaintCanvasDebug _debug;
        protected readonly PaintFrame _frame;
        protected readonly TriangleMesh _canvas;
        protected ImageLight? _light;

        public PaintCanvas(Quad3 quad, float texelSize = 0.001f, bool useMips = true)
        {
            LightRotation = 100f * MathF.PI / 180;
            DryRate = 0.3f;
            DensityToCoverage = 1.0f;
            NormalScale = 2.0f * 0.025f;
            DryRoughness = 0.5f;
            WetRoughness = 0.05f;
            DripRate = 0.1f;
            GravityStrength = 1.0f;
            SpraySpacing = 0.002f;
            PaintOpacityScale = 2.5f;
            SaveImageName = "d:\\canvas-saved.png";

            _size = quad.Size;
            _texelSize = texelSize;

            Texture2D CreateTexture() => useMips ? new()
            {
                MinFilter = ScaleFilter.LinearMipmapLinear,
                MipLevelCount = Utils.ComputeMaxMipLevel(
                    (int)(_size.X / _texelSize),
                    (int)(_size.Y / _texelSize), 128) + 1
            } : new();

            _sprayTexture = new Texture2D();
            _colorTexture = CreateTexture();
            _normalTexture = CreateTexture();
            _roughnessTexture = CreateTexture();

            _geo = new Quad3D(_size);

            _canvas = new TriangleMesh(_geo, new PbrMaterial()
            {
                ColorMap = _colorTexture,
                NormalMap = _normalTexture,
                MetallicRoughnessMap = _roughnessTexture,
                Alpha = AlphaMode.Blend
            });

            _canvas.Materials.Add(new DebugMaterial()
            {
                IsEnabled = false
            });

            _frame = new PaintFrame(_size, new PbrMaterial()
            {
                Color = new Color(0.0f, 0.0f, 0.7f),
            });

            AddChild(_canvas);
            AddChild(_frame);

            this.SetWorldPose(quad.Pose);

            this.AddComponent<SpatialAnchorGrid>();
        }

        public void SetCanvasSize(Vector2 newSize, Pose3 pose, float texelSize)
        {
            if (_geo is Quad3D quad)
            {
                quad.Size = newSize;
                quad.Build();
            }
            else if (_geo is BrickGeometry brick)
            {
                brick.WallSize = newSize;
                brick.Build();
            }

            _geo.NotifyChanged(ChangeType.Geometry);

            _frame.Size = newSize;
            _frame.Build();

            _size = newSize;

            _texelSize = texelSize;

            this.SetWorldPose(pose);
        }

        protected override void UpdateSelf(RenderContext ctx)
        {
            _light ??= _scene?.Descendants<ImageLight>().FirstOrDefault();

            if (_light != null)
            {
                var forward = Vector3.TransformNormal(Vector3.UnitZ, WorldMatrix);
                var rotation = LightRotation + MathF.Atan2(forward.X, forward.Z);

                if (rotation != _light.RotationY)
                {
                    _light.RotationY = rotation;
                    _light.NotifyChanged(ChangeType.Property);
                }
            }

            base.UpdateSelf(ctx);
        }

        Vector2 ComputeCanvasGravity()
        {
            Vector3 worldGravity = new(0.0f, 1.0f, 0.0f);

            var localGravity =
                Vector3.TransformNormal(worldGravity, WorldMatrixInverse);

            Vector2 g = new(localGravity.X, localGravity.Y);

            if (g.LengthSquared() > 0.000001f)
                g = Vector2.Normalize(g);

            return g;
        }

        [Action]
        public Task ClearAsync()
        {
            Cleared?.Invoke();
            return RenderDispatcher.ExecuteAsync(() => Simulation!.ClearCanvas());
        }

        [Action]
        public Task UndoAsync()
        {
            if (!CanUndo)
                return Task.CompletedTask;

            Undone?.Invoke();
            return RenderDispatcher.ExecuteAsync(() => Simulation!.Undo());
        }

        public Task DiscardTestStrokeAsync()
        {
            return RenderDispatcher.ExecuteAsync(() => Simulation!.Undo());
        }

        public Task<CanvasState> ReadStateAsync()
        {
            return RenderDispatcher.ExecuteAsync(() => Simulation!.ReadState());
        }

        public Task LoadStateAsync(CanvasState state)
        {
            return RenderDispatcher.ExecuteAsync(() => Simulation!.LoadState(state));
        }

        public void Update(GlUpdateContext ctx, ref PaintSimUniforms block)
        {
            _can ??= _scene!.Descendants<Can>().First()!;

            block.CanvasSize = new Vector2I((int)(_size.X / _texelSize), (int)(_size.Y / _texelSize));

            block.GravityCanvas = ComputeCanvasGravity();
            block.GravityStrength = GravityStrength;

            block.PaintColor = _can.Color.ToVector4();

            block.DeltaTime = ctx.DeltaTime;

            block.DryRoughness = DryRoughness;
            block.WetRoughness = WetRoughness;
            block.NormalScale = NormalScale;
            block.DryRate = DryRate;
            block.WetDripRate = DripRate;
            block.PaintOpacityScale = PaintOpacityScale;
        }

        protected void UpdateDebug()
        {
            var debugMat = (DebugMaterial)_canvas.Materials[1];

            debugMat.IsEnabled = Debug != PaintCanvasDebug.None;
            _canvas.Materials[0].IsEnabled = !debugMat.IsEnabled;

            if (debugMat.IsEnabled)
            {
                if (Debug == PaintCanvasDebug.Spray)
                {
                    debugMat.Texture = SprayTexture;
                }
                else if (Debug == PaintCanvasDebug.Wet)
                {
                    debugMat.Texture = PaintTextures![0];
                }
                else if (Debug == PaintCanvasDebug.Dry)
                {
                    debugMat.Texture = PaintTextures![1];
                }
            }

            debugMat.NotifyChanged(ChangeType.Material);
        }

        [Action]
        public void SaveImage()
        {
            var texture = _colorTexture.ToGlTexture().Read(TextureFormat.Rgba8);
            using var image = ImageUtils.ToBitmap(texture![0], false, SKAlphaType.Unpremul)!;
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var outStream = File.OpenWrite(SaveImageName!);
            data.SaveTo(outStream);

            Log.Info(this, "Image saved");
        }

        public string? SaveImageName { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float LightRotation { get; set; }

        public PaintFrame Frame => _frame;

        public Texture2D[]? PaintTextures { get; set; }

        public float TexelSize => _texelSize;

        public Vector2 Size => _size;

        public Texture2D SprayTexture => _sprayTexture;

        public Texture2D ColorTexture => _colorTexture;

        public Texture2D RoughnessTexture => _roughnessTexture;

        public Texture2D NormalTexture => _normalTexture;

        public float DensityToCoverage { get; set; }

        public float GravityStrength { get; set; }

        public float DryRate { get; set; }

        public float DripRate { get; set; }

        public float DryRoughness { get; set; }

        public float WetRoughness { get; set; }

        public float NormalScale { get; set; }

        [Range(0, 10, 0.1f)]
        public float PaintOpacityScale { get; set; }

        [Range(0, 1, 0.001f)]
        public float SpraySpacing { get; set; }

        public event Action? Cleared;

        public event Action? Undone;

        static IDispatcher RenderDispatcher => EngineApp.Current!.Renderer.Dispatcher;

        internal GlSimulationPass? Simulation { get; set; }

        public bool CanUndo { get; internal set; }

        public PaintCanvasDebug Debug
        {
            get => _debug;
            set
            {
                _debug = value;
                UpdateDebug();
            }
        }

    }
}

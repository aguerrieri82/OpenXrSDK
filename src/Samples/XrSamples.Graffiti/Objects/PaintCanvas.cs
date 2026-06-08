using SkiaSharp;
using System.Numerics;
using XrEngine;
using XrMath;
using XrSamples.Graffiti.Shaders;
using XrEngine.OpenGL;

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
        private Vector2 _size;
        readonly Texture2D _colorTexture;
        readonly Texture2D _normalTexture;
        readonly Texture2D _roughnessTexture;
        readonly Texture2D _sprayTexture;
        private readonly TriangleMesh _quad;
        private float _texelSize;
        private Can? _can;
        private PaintCanvasDebug _debug;
        private readonly PaintFrame _frame;

        public PaintCanvas(Quad3 quad, float texelSize = 0.001f, bool useMips = true)
        {
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

            _quad = new TriangleMesh(new Quad3D(_size), new PbrV2Material()
            {
                ColorMap = _colorTexture,
                NormalMap = _normalTexture,
                MetallicRoughnessMap = _roughnessTexture,
                Alpha = AlphaMode.Blend
            });

            _quad.Materials.Add(new DebugMaterial()
            {
                IsEnabled = false
            });

            _frame = new PaintFrame(_size, new PbrV2Material()
            {
                Color = new Color(0.0f, 0.0f, 0.7f),
            });

            AddChild(_quad);
            AddChild(_frame);

            this.SetWorldPose(quad.Pose);
        }

        public void SetCanvasSize(Vector2 newSize, Pose3 pose, float texelSize)
        {
            var quad3D = (Quad3D)_quad.Geometry!;

            quad3D.Size = newSize;
            quad3D.Build();
            quad3D.NotifyChanged(ObjectChangeType.Geometry);

            _frame.Size = newSize;
            _frame.Build();

            _size = newSize;

            _texelSize = texelSize;

            this.SetWorldPose(pose);
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
        public void Clear()
        {
            ClearRequest = true;
        }


        [Action]
        public void Undo()
        {
            UndoRequest = true;
        }

        public void Update(RenderContext ctx, ref PaintSimUniforms block)
        {
            _can ??= _scene!.Descendants<Can>().First()!;

            block.CanvasSize = new Vector2I((int)(_size.X / _texelSize), (int)(_size.Y / _texelSize));

            block.GravityCanvas = ComputeCanvasGravity();
            block.GravityStrength = GravityStrength;

            block.PaintColor = _can.Color.ToVector4();

            block.DeltaTime = (float)ctx.DeltaTime;

            block.DryRoughness = DryRoughness;
            block.WetRoughness = WetRoughness;
            block.NormalScale = NormalScale;
            block.DryRate = DryRate;
            block.WetDripRate = DripRate;
            block.PaintOpacityScale = PaintOpacityScale;
        }


        protected void UpdateDebug()
        {
            var debugMat = (DebugMaterial)_quad.Materials[1];

            debugMat.IsEnabled = Debug != PaintCanvasDebug.None;
            _quad.Materials[0].IsEnabled = !debugMat.IsEnabled;

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

            debugMat.NotifyChanged(ObjectChangeType.Material);
        }


        [Action]
        public async Task SaveImage()
        {
            _ = _scene!.App!.Renderer!.Dispatcher.ExecuteAsync(() =>
            {
                var texture = _colorTexture.ToGlTexture().Read(TextureFormat.Rgba32);
                using var image = ImageUtils.ToBitmap(texture![0], false, SKAlphaType.Unpremul)!;
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                using var outStream = File.OpenWrite(SaveImageName!);
                data.SaveTo(outStream);

                Log.Info(this, "Image saved");
            });
  
        }

        public string? SaveImageName { get; set; }

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

        internal bool ClearRequest { get; set; }

        internal bool UndoRequest { get; set; }

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

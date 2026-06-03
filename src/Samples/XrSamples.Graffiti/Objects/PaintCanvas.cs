using System.Numerics;
using XrEngine;
using XrMath;
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
        private Vector2 _size;
        readonly Texture2D _colorTexture;
        readonly Texture2D _normalTexture;
        readonly Texture2D _roughnessTexture;
        readonly Texture2D _sprayTexture;
        private readonly TriangleMesh _quad;
        private readonly float _texelSize;
        private Can? _can;
        private PaintCanvasDebug _debug;
        private readonly PaintFrame _frame;

        public PaintCanvas(Quad3 quad, float texelSize = 0.001f)
        {
            DryRate = 0.75f;
            DensityToCoverage = 1.0f;
            DensityToHeight = 0.025f;
            NormalScale = 2.0f;
            DryRoughness = 0.9f;
            WetRoughness = 0.05f;
            DensityScale = 1f;
            DripRate = 0.1f;
            GravityStrength = 1.0f;

            _size = quad.Size;
            _texelSize = texelSize;

            _sprayTexture = new Texture2D();
            _colorTexture = new Texture2D();
            _normalTexture = new Texture2D();
            _roughnessTexture = new Texture2D();

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

        public void SetCanvasSize(Vector2 newSize, Pose3 pose)
        {
            var quad3D = (Quad3D)_quad.Geometry!;

            quad3D.Size = newSize;
            quad3D.Build();
            quad3D.NotifyChanged(ObjectChangeType.Geometry);

            _frame.Size = newSize;
            _frame.Build();

            _size = newSize;

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

        public void Update(RenderContext ctx, ref PaintSimUniforms block)
        {
            _can ??= _scene!.Descendants<Can>().First()!;

            block.CanvasSize = new Vector2I((int)(_size.X / _texelSize), (int)(_size.Y / _texelSize));

            block.GravityCanvas = ComputeCanvasGravity();
            block.GravityStrength = GravityStrength;

            block.PaintColor = _can.Color.ToVector4();
            block.PaintColor.W *= DensityScale;

            block.DeltaTime = (float)ctx.DeltaTime;

            block.DryRoughness = DryRoughness;
            block.WetRoughness = WetRoughness;

            block.NormalScale = NormalScale;
            block.DensityToHeight = DensityToHeight;

            block.DensityToCoverage = DensityToCoverage;

            block.DryRate = DryRate;

            block.WetDripRate = DripRate;
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

        public PaintFrame Frame => _frame;

        public Texture2D[]? PaintTextures { get; set; }

        public float TexelSize => _texelSize;

        public Vector2 Size => _size;

        public Texture2D SprayTexture => _sprayTexture;

        public Texture2D ColorTexture => _colorTexture;

        public Texture2D RoughnessTexture => _roughnessTexture;

        public Texture2D NormalTexture => _normalTexture;

        public float DensityToCoverage { get; set; }

        public float DensityScale { get; set; }

        public float GravityStrength { get; set; }

        public float DryRate { get; set; }

        public float DripRate { get; set; }

        public float DryRoughness { get; set; }

        public float WetRoughness { get; set; }

        public float NormalScale { get; set; }

        public float DensityToHeight { get; set; }

        internal bool ClearRequest { get; set; }

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

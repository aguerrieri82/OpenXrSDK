using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{

    public class PaintCanvas : Group3D
    {
        readonly IList<PaintLayerParams> _layers = [];
        readonly Vector2 _size;
        readonly Texture2D _colorTexture;
        readonly Texture2D _normalTexture;
        readonly Texture2D _roughnessTexture;
        readonly Texture2D _sprayTexture;
        private TriangleMesh _quad;
        private float _texelSize;
        private Can? _can;

        public PaintCanvas(Quad3 quad, float texelSize = 0.001f, int layers = 3)
        {
            SprayDensityScale = 1.0f;

            GravityStrength = 1.0f;

            GlobalDryScale = 1.0f;
            GlobalDripScale = 0.1f;
            GlobalMixScale = 1.0f;

            DryRoughness = 0.75f;
            WetRoughness = 0.18f;

            HeightScale = 2.0f;
            DensityToHeight = 0.05f;

            _size = quad.Size;
            _texelSize = texelSize;

            var texData = new TextureData
            {
                Format = TextureFormat.RgbaFloat16,
                Width = (uint)(_size.X / texelSize),
                Height = (uint)(_size.Y / texelSize),
            };

            _sprayTexture = new Texture2D();
            _sprayTexture.LoadData(texData);

            _colorTexture = new Texture2D();
            _colorTexture.LoadData(texData);

            _normalTexture = new Texture2D();
            _normalTexture.LoadData(texData);

            _roughnessTexture = new Texture2D();
            _roughnessTexture.LoadData(texData);

            _quad = new TriangleMesh(new Quad3D(_size), new PbrV2Material()
            {
                ColorMap = _colorTexture,
                NormalMap = _normalTexture,
                MetallicRoughnessMap = _roughnessTexture,
                Alpha = AlphaMode.Blend
            });

            AddChild(_quad);

            float layerStep = 1.0f / (layers - 1);

            for (var i = 0; i < layers; i++)
                AddLayer(texelSize, i * layerStep);

            this.SetWorldPose(quad.Pose);
        }

        Vector2 ComputeCanvasGravity()
        {
            Vector3 worldGravity = new(0.0f, 1.0f, 0.0f);


            Vector3 localGravity =
                Vector3.TransformNormal(worldGravity, WorldMatrixInverse);

            Vector2 g = new(localGravity.X, localGravity.Y);

            if (g.LengthSquared() > 0.000001f)
                g = Vector2.Normalize(g);

            return g;
        }

        public void Update(RenderContext ctx, ref PaintSimulationBlock block)
        {
            _can ??= _scene.Descendants<Can>().First()!;

            block.CanvasSize = _size;
            block.GravityCanvas = ComputeCanvasGravity();
            block.LayerCount = _layers.Count;
            for (var i = 0; i < _layers.Count; i++)
                block.Layers[i] = _layers[i];

            block.SprayColor = _can.Color.ToVector3();
            block.DeltaTime = (float)ctx.DeltaTime;

            block.SprayDensityScale = SprayDensityScale;
            block.GravityStrength = GravityStrength;

            block.GlobalDryScale = GlobalDryScale;
            block.GlobalDripScale = GlobalDripScale;
            block.GlobalMixScale = GlobalMixScale;

            block.DryRoughness = DryRoughness;
            block.WetRoughness = WetRoughness;

            block.HeightScale = HeightScale;
            block.DensityToHeight = DensityToHeight;

        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        protected void AddLayer(float texelSize, float dryness)
        {
            dryness = Math.Clamp(dryness, 0.0f, 1.0f);

            float wetness = 1.0f - dryness;

            float wetDripWorldPerSec = 0.015f; // tune: canvas units/sec
            float dryDripWorldPerSec = 0.0f;

            float dripWorldPerSec = Lerp(wetDripWorldPerSec, dryDripWorldPerSec, dryness);
            float dripTexelsPerSec = dripWorldPerSec / Math.Max(_texelSize, 0.000001f);

            var layer = new PaintLayerParams
            {
                DryRateToNext = Lerp(0.65f, 0.0f, dryness),
                Wetness = wetness,
                DripRate = dripTexelsPerSec,
                DripThreshold = Lerp(0.75f, 999.0f, dryness),
                MixStrength = Lerp(1.0f, 0.0f, dryness),
                StainStrength = Lerp(0.2f, 1.0f, dryness),
            };

            _layers.Add(layer); 
        }

        public IList<PaintLayerParams> Layers => _layers;

        public float TexelSize => _texelSize;

        public Vector2 Size => _size;

        public Texture2D SprayTexture => _sprayTexture;

        public Texture2D ColorTexture => _colorTexture;

        public Texture2D RoughnessTexture => _roughnessTexture;

        public Texture2D NormalTexture => _normalTexture;

        public float SprayDensityScale { get; set; }

        public float GravityStrength { get; set; }

        public float GlobalDryScale { get; set; }

        public float GlobalDripScale { get; set; }

        public float GlobalMixScale { get; set; }

        public float DryRoughness { get; set; }

        public float WetRoughness { get; set; }

        public float HeightScale { get; set; }

        public float DensityToHeight { get; set; }

    }
}

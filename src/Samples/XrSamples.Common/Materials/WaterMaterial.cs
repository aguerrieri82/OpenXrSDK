using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples
{
    public sealed class WaterMaterial : PbrMaterial
    {
        private static readonly ResourceSlot WaterStateSlot = new("WaterState");

        public WaterMaterial(uint simulationSize)
        {
            StateTexture = new Texture2D
            {
                Name = "Water state",
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MinFilter = ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                NeverCompress = true
            };
            StateTexture.SetDescription(simulationSize, simulationSize, 2, TextureFormat.RgbaFloat16);

            Color = new Color(0.025f, 0.22f, 0.32f, 0.58f);
            Alpha = AlphaMode.Opaque;
            TransmissionMode = TransmissionMode.Texture;
            Transmission = 1;
            Ior = 1.5f;
            Thickness = 0.02f;
            WriteDepth = false;
            DoubleSided = true;
            CastShadows = false;
            UseEnvDepth = true;
            Roughness = 0.08f;
            Metalness = 0f;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            base.UpdateShaderMaterial(bld);

            var stateSlot = bld.GetTextureSlot(WaterStateSlot);

            bld.AddFeature("USE_TANGENTS");
            bld.SetVsIncludes("[XrSamples.Common]Water/water_vertex.glsl");
            bld.SetFsIncludes("[XrSamples.Common]Water/water_pbr.glsl");
            bld.SetVertexLocalTransform("applyWaterVertex(position, normal, tangent, aUv0);");
            bld.SetFragmentLoader("frag = loadWaterFragmentProperties();");

            bld.ExecuteAction((ctx, uniforms) =>
            {
                uniforms.SetUniform("uWaterState", StateTexture, stateSlot);
                uniforms.SetUniform("uWaterLayer", CurrentLayer);
                uniforms.SetUniform("uWaterSize", WaterSize);
                uniforms.SetUniform("uWaterTexelSize", new Vector2(1f / StateTexture.Width, 1f / StateTexture.Height));
                uniforms.SetUniform("uSurfaceHeightScale", SurfaceHeightScale);
                uniforms.SetUniform("uFineRippleNormalStrength", FineRippleNormalStrength);
                uniforms.SetUniform("uWaterDepth", WaterDepth);
            });
        }

        public override void Dispose()
        {
            StateTexture.Dispose();
            base.Dispose();
        }

        public Texture2D StateTexture { get; }

        public int CurrentLayer { get; internal set; }

        public Vector2 WaterSize { get; set; }

        public float SurfaceHeightScale { get; set; }

        public float FineRippleNormalStrength { get; set; }

        public float WaterDepth { get; set; }
    }
}

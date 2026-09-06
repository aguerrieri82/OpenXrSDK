using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples
{
    public sealed class WaterMaterial : PbrMaterial
    {
        private static readonly ResourceSlot WaterStateSlot = new("WaterState");

        public WaterMaterial(Texture2D stateTexture)
        {
            StateTexture = stateTexture;

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
                uniforms.SetUniform("uWaterHeightScale", HeightScale);
                uniforms.SetUniform("uAmbientWaveHeight", AmbientWaveHeight);
                uniforms.SetUniform("uAmbientWaveScale", AmbientWaveScale);
                uniforms.SetUniform("uWakeDetailStrength", WakeDetailStrength);
                uniforms.SetUniform("uWaterDepth", WaterDepth);
                uniforms.SetUniform("uWaterTime", ctx.Time);
            });
        }

        public Texture2D StateTexture { get; }

        public int CurrentLayer { get; internal set; }

        public Vector2 WaterSize { get; set; }

        public float HeightScale { get; set; }

        public float AmbientWaveHeight { get; set; }

        public float AmbientWaveScale { get; set; }

        public float WakeDetailStrength { get; set; }

        public float WaterDepth { get; set; }
    }
}

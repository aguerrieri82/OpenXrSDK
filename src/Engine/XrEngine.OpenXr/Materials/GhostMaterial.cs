using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.OpenXr
{
    public class GhostMaterial : ShaderMaterial
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Uniforms
        {
            public Vector4 Color;
            public Vector4 RimColor;
            public Vector4 CameraPos;

            public float FillAlpha;
            public float RimStart;
            public float RimEnd;
            public float RimPower;
        }

        public static readonly Shader SHADER;

        static GhostMaterial()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "[XrEngine.OpenXr]ghost.frag",
                IsLit = false
            };
        }

        public GhostMaterial()
            : base()
        {
            _shader = SHADER;

            Color = new Color(1f, 0f, 0f, 1f);
            RimColor = new Color(1f, 0.25f, 0.1f, 1f);

            FillAlpha = 0.08f;
            RimStart = 0.35f;
            RimEnd = 0.85f;
            RimPower = 1.5f;
            UseDepth = true;
            WriteDepth = false;
            Alpha = AlphaMode.Blend;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("USE_CAMERA_POS");
            bld.AddFeature("USE_SKIN");

            bld.LoadBuffer<Uniforms>((ctx, ref update) =>
            {
                update.Value = new Uniforms
                {
                    Color = Color,
                    FillAlpha = FillAlpha,
                    RimColor = RimColor,
                    RimEnd = RimEnd,
                    RimPower = RimPower,
                    RimStart = RimStart,
                };

                return true;

            }, 16, BufferStore.Material);
        }

        public Color Color { get; set; }

        public Color RimColor { get; set; }

        public float FillAlpha { get; set; }

        public float RimStart { get; set; }

        public float RimEnd { get; set; }

        public float RimPower { get; set; }
    }
}

using XrMath;

namespace XrEngine
{
    public class GlowVolumeSliceMaterial : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static GlowVolumeSliceMaterial()
        {
            SHADER = new StandardVertexShader
            {
                
                FragmentSourceName = "glow_vol_slice.frag",
                GeometrySourceName = "Shared/cube_volume.geom",
                VertexSourceName = "pass.vert",
                IsLit = false
            };
        }


        public GlowVolumeSliceMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            Slices = 20;
            Alpha = AlphaMode.Blend;
            UseDepth = false;
            WriteDepth = false;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uSphereCenter", ctx.Model!.WorldPosition);
                up.SetUniform("uSphereRadius", SphereRadius);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("uHaloWidth", HaloWidth);
                up.SetUniform("uHaloColor", HaloColor);
                up.SetUniform("uNumSlices", Slices);
            });
        }

        public float SphereRadius { get; set; }

        public float HaloWidth { get; set; }

        public Color HaloColor { get; set; }

        [Range(1, 100, 1)]
        public int Slices { get; set; }
    }
}
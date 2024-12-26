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
                GeometrySourceName = "cube_volume.geom",
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
                up.SetUniform("uCameraForward", ctx.MainCamera!.Forward);
                up.SetUniform("uCameraUp", ctx.MainCamera.Up);
            });
        }

        public float SphereRadius { get; set; }

        public float HaloWidth { get; set; }

        public Color HaloColor { get; set; }

        [Range(1, 100, 1)]
        public int Slices { get; set; }
    }
}
using XrMath;

namespace XrEngine.Materials
{
    public class CubeMapMaterial : ShaderMaterial
    {
        static readonly Shader SHADER = new()
        {
            FragmentSourceName = "pbr/cubemap.frag",
            VertexSourceName = "pbr/cubemap.vert",
            Priority = -1,
            Resolver = str => Embedded.GetString(str),
            IsLit = false
        };

        public CubeMapMaterial()
        {
            Shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = false;
            DoubleSided = false;

            Exposure = 1;
            Intensity = 1;
            LinearOutput = true;
            Blur = 0; //0.6 
            Rotation = 0;
            MipCount = 1;

        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<CubeMapMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<CubeMapMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("UNIFORM_EXP");

            if (LinearOutput)
                bld.AddFeature("LINEAR_OUTPUT");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uGGXEnvSampler", Texture!, 0);
                up.SetUniform("uMipCount", MipCount);
                up.SetUniform("uEnvBlurNormalized", Blur);
                up.SetUniform("uEnvIntensity", Intensity);
                up.SetUniform("uViewProjectionMatrix", ctx.Camera!.ViewProjection);
                up.SetUniform("uExposure", Exposure);
                up.SetUniform("uEnvRotation", Matrix3x3.CreateRotationY(Rotation));
            });
        }

        public TextureCube? Texture { get; set; }

        public int MipCount { get; set; }

        public float Intensity { get; set; }

        public float Blur { get; set; }

        public float Exposure { get; set; }

        public float Rotation { get; set; }

        public bool LinearOutput { get; set; }
    }
}

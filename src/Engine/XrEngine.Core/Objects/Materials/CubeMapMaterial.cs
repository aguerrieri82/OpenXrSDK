using XrMath;

namespace XrEngine.Materials
{
    public class CubeMapMaterial : ShaderMaterial
    {
        static readonly Shader SHADER = new()
        {
            FragmentSourceName = "cubemap.frag",
            VertexSourceName = "cubemap.vert",
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
            Rotation = 0;
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

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("UNIFORM_EXP");

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uCube", Texture!, 0);
                up.SetUniform("uCubeRotation", Matrix3x3.CreateRotationY(Rotation));
            });
        }


        public TextureCube? Texture { get; set; }

        public float Rotation { get; set; }
    }
}

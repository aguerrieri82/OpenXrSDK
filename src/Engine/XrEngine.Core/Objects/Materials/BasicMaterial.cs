using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class BasicMaterial : ShaderMaterial, IColorSource
    {
        static readonly Shader SHADER;

        static BasicMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "basic.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = true,
                UpdateHandler = StandardVertexShaderHandler.Instance
            };
        }

        public BasicMaterial()
        {
            Specular.Rgb(0.5f);
            Ambient = Color.White;
            Shininess = 32f;
            Shader = SHADER;
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.WriteObject<BasicMaterial>(this);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            container.ReadObject<BasicMaterial>(this);
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {

            if (DiffuseTexture != null)
            {
                bld.AddFeature("TEXTURE");
                bld.SetUniform("uTexture0", (ctx) => DiffuseTexture, 0);
            }

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("material.ambient", (Vector3)Ambient);
                up.SetUniform("material.diffuse", (Vector3)Color);
                up.SetUniform("material.specular", (Vector3)Specular);
                up.SetUniform("material.shininess", Shininess);
            });

        }



        public Texture2D? DiffuseTexture { get; set; }

        public Color Color { get; set; }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}

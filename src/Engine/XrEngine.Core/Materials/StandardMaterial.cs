using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class StandardMaterial : ShaderMaterial, IColorSource
    {
        static readonly Shader SHADER;

        static StandardMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "standard.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = true
            };
        }

        public StandardMaterial()
        {
            Specular.Rgb(0.5f);
            Ambient = Color.White;
            Shininess = 32f;
            Shader = SHADER;
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
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("material.ambient", (Vector3)Ambient);
                up.SetUniform("material.diffuse", (Vector3)Color);
                up.SetUniform("material.specular", (Vector3)Specular);
                up.SetUniform("material.shininess", Shininess);
            });

        }

        public static readonly IShaderHandler GlobalHandler = StandardVertexShaderHandler.Instance;

        public Texture2D? DiffuseTexture { get; set; }

        public Color Color { get; set; }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}

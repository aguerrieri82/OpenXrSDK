using System.Numerics;

namespace Xr.Engine
{
    public class StandardMaterial : ShaderMaterial
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


            bld.SetUniform("material.ambient", (ctx) => (Vector3)Ambient);
            bld.SetUniform("material.diffuse", (ctx) => (Vector3)Color);
            bld.SetUniform("material.specular", (ctx) => (Vector3)Specular);
            bld.SetUniform("material.shininess", (ctx) => Shininess);

            StandardVertexShaderHandler.Instance.UpdateShader(bld);
        }

        public Texture2D? DiffuseTexture { get; set; }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}

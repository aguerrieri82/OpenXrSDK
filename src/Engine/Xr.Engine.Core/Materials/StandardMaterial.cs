using System.Numerics;

namespace OpenXr.Engine
{
    public class StandardMaterial : ShaderMaterial, IShaderHandler
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

        public override void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {
            if (DiffuseTexture != null)
                fl.AddFeature("TEXTURE");

            up.SetUniform("material.ambient", (Vector3)Ambient);
            up.SetUniform("material.diffuse", (Vector3)Color);
            up.SetUniform("material.specular", (Vector3)Specular);
            up.SetUniform("material.shininess", Shininess);

            ctx.UpdateStandardVS(up, fl);
        }

        public Texture2D? DiffuseTexture { get; set; } 

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}

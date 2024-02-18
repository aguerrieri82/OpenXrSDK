using System.Numerics;

namespace OpenXr.Engine
{
    public class StandardMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static StandardMaterial()
        {
            SHADER = new Shader
            {
                FragmentSource = Embedded.GetString("standard_fs.glsl"),
                VertexSource = Embedded.GetString("standard_vs.glsl"),
                IncludeResolver = str => Embedded.GetString(str),
                IsLit = true
            };
        }

        public StandardMaterial()
        {
            Specular.Rgb(0.5f);
            Shininess = 32f;
            Shader = SHADER;
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("material.ambient", (Vector3)Ambient);
            obj.SetUniform("material.diffuse", (Vector3)Color);
            obj.SetUniform("material.specular", (Vector3)Specular);
            obj.SetUniform("material.shininess", 32.0f);

            base.UpdateUniforms(obj);
        }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}

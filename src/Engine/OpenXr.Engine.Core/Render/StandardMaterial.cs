namespace OpenXr.Engine
{
    public class StandardMaterial : ShaderMaterial
    {
        static readonly Shader _shader;

        static StandardMaterial()
        {
            _shader = new Shader();
            _shader.FragmentSource = Embedded.GetString("standard_fs.glsl");
            _shader.VertexSource = Embedded.GetString("standard_vs.glsl");
        }

        public StandardMaterial()
        {
            Specular.Rgb(0.5f);
            Shininess = 32f;
            Shader = _shader;
        }


        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("material.ambient", Ambient);
            obj.SetUniform("material.diffuse", Color);
            obj.SetUniform("material.specular", Specular);
            obj.SetUniform("material.shininess", 32.0f);

            base.UpdateUniforms(obj);
        }

        public Color Ambient { get; set; }

        public Color Specular { get; set; }

        public float Shininess { get; set; }

    }
}

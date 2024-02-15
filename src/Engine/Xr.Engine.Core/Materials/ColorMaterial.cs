namespace OpenXr.Engine
{
    public class ColorMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static ColorMaterial()
        {
            SHADER = new Shader
            {
                FragmentSource = Embedded.GetString("color_fs.glsl"),
                VertexSource = Embedded.GetString("standard_vs.glsl"),
                IsLit = false
            };
        }


        public ColorMaterial()
            : base()
        {
            _shader = SHADER;
        }


        public ColorMaterial(Color color)
            : this()
        {
            Color = color;
        }


        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("uColor", Color);
        }

    }
}

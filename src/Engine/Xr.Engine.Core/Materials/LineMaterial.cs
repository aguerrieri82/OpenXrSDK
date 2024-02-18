namespace OpenXr.Engine
{
    public class LineMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static LineMaterial()
        {
            SHADER = new Shader
            {
                FragmentSource = Embedded.GetString("line_fs.glsl"),
                VertexSource = Embedded.GetString("line_vs.glsl"),
                IncludeResolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public LineMaterial()
            : base()
        {
            _shader = SHADER;
        }


        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetLineSize(1);
        }
    }
}

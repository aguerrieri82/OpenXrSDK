namespace OpenXr.Engine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSource = Embedded.GetString("color_fs.glsl"),
                VertexSource = Embedded.GetString("standard_vs.glsl"),
                IncludeResolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public DepthOnlyMaterial()
            : base()
        {
            _shader = SHADER;
            WriteColor = false;
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("color", Color.Transparent);
        }

        public static readonly DepthOnlyMaterial Instance = new DepthOnlyMaterial();
    }
}

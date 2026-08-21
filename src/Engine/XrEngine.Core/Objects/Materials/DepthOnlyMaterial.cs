namespace XrEngine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new StandardShader
            {
                FragmentSourceName = "empty.frag",
                IsLit = false
            };
        }

        public DepthOnlyMaterial()
            : base()
        {
            _shader = SHADER;
            WriteColor = false;
            WriteDepth = true;
        }
    }
}

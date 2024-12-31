namespace XrEngine
{
    public class DepthClearEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static DepthClearEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "clear.frag",
                VertexSourceName = "fullscreen.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                Priority = -1
            };
        }


        public DepthClearEffect()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = true;
            WriteColor = false;
        }

    }
}

namespace XrEngine
{
    public class DepthCopyEffect : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static DepthCopyEffect()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "copy_depth.frag",
                VertexSourceName = "fullscreen.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                Priority = -1
            };
        }


        public DepthCopyEffect()
            : base()
        {
            _shader = SHADER;
            Alpha = AlphaMode.Opaque;
            UseDepth = false;
            WriteDepth = false;
        }

    }
}

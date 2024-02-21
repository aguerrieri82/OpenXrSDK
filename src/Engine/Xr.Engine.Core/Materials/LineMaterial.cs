namespace OpenXr.Engine
{
    public class LineMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static LineMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "line.frag",
                VertexSourceName = "line.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false
            };
        }


        public LineMaterial()
            : base()
        {
            _shader = SHADER;
        }
    }
}

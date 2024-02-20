namespace OpenXr.Engine
{
    public class DepthViewMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthViewMaterial()
        {
            SHADER = new Shader
            {
                FragmentSourceName = "depth.frag",
                VertexSourceName = "standard.vert",
                Resolver = str => Embedded.GetString(str),
                IsLit = false,
                Priority = 1
            };
        }


        public DepthViewMaterial()
            : base()
        {
            _shader = SHADER;
   
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            var renderer = _host?.Scene?.App?.Renderer;

            var depth = renderer?.GetDepth();
            if (depth != null)
            {
                if (depth.SampleCount <= 1)
                    obj.SetUniform("uTexture0", depth, 0);
                else
                    obj.SetUniform("uTexture0MS", depth, 0);

                obj.SetUniform("uSamples", depth.SampleCount);

            }

            var camera = _host?.Scene?.ActiveCamera;
            if (camera != null)
            {
                obj.SetUniform("uNearPlane", camera.Near);
                obj.SetUniform("uFarPlane", camera.Far);
            }

        }
    }
}

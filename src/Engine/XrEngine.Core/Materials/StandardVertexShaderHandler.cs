using System.Numerics;

namespace XrEngine
{
    public class StandardVertexShaderHandler : IShaderHandler
    {
        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ctx.Camera!;

                up.SetUniform("uView", camera.View);
                up.SetUniform("uProjection", camera.Projection);
                up.SetUniform("uViewPos", camera.Transform.Position, true);
            });


            foreach (var light in bld.Context.Lights!)
            {
                if (light is AmbientLight ambient)
                    bld.SetUniform("light.ambient", (ctx) => (Vector3)ambient.Color * ambient.Intensity);

                else if (light is PointLight point)
                {
                    bld.SetUniform("light.diffuse", (ctx) => (Vector3)point.Color * point.Intensity);
                    bld.SetUniform("light.position", (ctx) => point.WorldPosition);
                    bld.SetUniform("light.specular", (ctx) => (Vector3)point.Specular);
                }
            }
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
        {
            return true;
        }

        public static readonly StandardVertexShaderHandler Instance = new StandardVertexShaderHandler();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class StandardVertexShaderHandler : IShaderHandler
    {
        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            if (bld.Context.Camera != null)
            {
                bld.SetUniform("uView", (ctx) => ctx.Camera!.View);
                bld.SetUniform("uProjection", (ctx) => ctx.Camera!.Projection);
                bld.SetUniform("uViewPos", (ctx) => ctx.Camera!.Transform.Position, true);
            }

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

            if (bld.Context.Model != null)
                bld.SetUniform("uModel", (ctx) => ctx.Model!.WorldMatrix);
        }

        public static readonly StandardVertexShaderHandler Instance = new StandardVertexShaderHandler();
    }
}

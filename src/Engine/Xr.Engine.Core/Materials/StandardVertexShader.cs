using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public static class StandardVertexShader
    {
        public static void UpdateStandardVS(this UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl)
        {
            if (ctx.Camera != null)
            {
                up.SetUniform("uView", ctx.Camera.Transform.Matrix);
                up.SetUniform("uProjection", ctx.Camera.Projection);
                up.SetUniform("uViewPos", ctx.Camera.Transform.Position, true);
            }

            foreach (var light in ctx.Lights!)
            {
                if (light is AmbientLight ambient)
                    up.SetUniform("light.ambient", (Vector3)ambient.Color * ambient.Intensity);

                else if (light is PointLight point)
                {
                    up.SetUniform("light.diffuse", (Vector3)point.Color * point.Intensity);
                    up.SetUniform("light.position", point.WorldPosition);
                    up.SetUniform("light.specular", (Vector3)point.Specular);
                }
            }

            if (ctx.Model != null)
                up.SetUniform("uModel", ctx.Model.WorldMatrix);
        }
    }
}

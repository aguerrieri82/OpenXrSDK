using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Materials;
using XrMath;

namespace XrEngine
{
    public class EnvironmentView : TriangleMesh
    {
        static readonly Geometry3D CubeGeometry = new()
        {
            Indices = [
                1, 2, 0,
                2, 3, 0,
                6, 2, 1,
                1, 5, 6,
                6, 5, 4,
                4, 7, 6,
                6, 3, 2,
                7, 3, 6,
                3, 7, 0,
                7, 4, 0,
                5, 1, 0,
                4, 5, 0
            ],
            Vertices = VertexData.FromPos(
            [
                -1, -1, -1,
                 1, -1, -1,
                 1,  1, -1,
                -1,  1, -1,
                -1, -1,  1,
                 1, -1,  1,
                 1,  1,  1,
                -1,  1,  1
            ]),
            ActiveComponents = VertexComponent.Position
        };

        public class EnvViewMaterial : CubeMapMaterial
        {
            public override void UpdateShader(ShaderUpdateBuilder bld)
            {
                bld.AddFeature("UNIFORM_EXP");

                if (PbrMaterial.LinearOutput)
                    bld.AddFeature("LINEAR_OUTPUT");

                bld.AddFeature(PbrMaterial.ToneMap.ToString());

                bld.ExecuteAction((ctx, up) =>
                {
                    var image = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();
                    var textures = image?.Textures;

                    if (image != null && textures != null && ctx.Camera != null)
                    {
                        up.SetUniform("u_GGXEnvSampler", textures.Env!, 0);
                        up.SetUniform("u_MipCount", (int)textures.MipCount);
                        up.SetUniform("u_EnvBlurNormalized", Blur);
                        up.SetUniform("u_EnvIntensity", image.Intensity);
                        up.SetUniform("u_ViewProjectionMatrix", ctx.Camera.View * ctx.Camera.Projection);
                        up.SetUniform("u_Exposure", ctx.Camera.Exposure);
                        up.SetUniform("u_EnvRotation", Matrix3x3.Rotation(image.Rotation));
                    }
                });
            }
        }

        public EnvironmentView()
        {
            Geometry = CubeGeometry;
            Materials.Add(new EnvViewMaterial() { });
        }
    }
}

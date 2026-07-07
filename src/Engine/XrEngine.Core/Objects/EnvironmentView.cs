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
            public EnvViewMaterial()
            {
                UseDepth = true;
            }

            protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
            {
                bld.AddFeature("COLOR_CORRECT");
                bld.AddFeature("MIP_FACTOR");


                bld.ExecuteAction((ctx, up) =>
                {
                    var light = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();
                    var textures = light?.Textures;

                    if (light != null && textures?.Env != null && ctx.PassCamera != null)
                    {
                        up.LoadTexture(textures.Env, 0);

                        up.SetUniform("uMipCount", (int)textures.MipCount);
                        up.SetUniform("uMipFactor", Blur);
                        up.SetUniform("uIntensity", light.Intensity * ctx.PassCamera.Exposure);
                        up.SetUniform("uCubeRotation", Matrix3x3.CreateRotationY(light.RotationY));

                    }
                });
            }

            public float Blur { get; set; }
        }

        public EnvironmentView()
        {
            Geometry = CubeGeometry;
            Materials.Add(new EnvViewMaterial() { });
        }
    }
}

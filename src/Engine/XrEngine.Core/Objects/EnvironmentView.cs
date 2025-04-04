﻿using XrEngine.Materials;
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
                bld.AddFeature("UNIFORM_EXP");

                if (PbrV1Material.LinearOutput)
                    bld.AddFeature("LINEAR_OUTPUT");

                bld.AddFeature(PbrV1Material.ToneMap.ToString());

                bld.ExecuteAction((ctx, up) =>
                {
                    var image = ctx.Lights?.OfType<ImageLight>().FirstOrDefault();
                    var textures = image?.Textures;

                    if (image != null && textures != null && ctx.PassCamera != null)
                    {
                        up.SetUniform("uGGXEnvSampler", textures.Env!, 0);
                        up.SetUniform("uMipCount", (int)textures.MipCount);
                        up.SetUniform("uEnvBlurNormalized", Blur);
                        up.SetUniform("uEnvIntensity", image.Intensity);
                        up.SetUniform("uViewProjectionMatrix", ctx.PassCamera.ViewProjection);
                        up.SetUniform("uExposure", ctx.PassCamera.Exposure);
                        up.SetUniform("uEnvRotation", Matrix3x3.CreateRotationY(image.RotationY));
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

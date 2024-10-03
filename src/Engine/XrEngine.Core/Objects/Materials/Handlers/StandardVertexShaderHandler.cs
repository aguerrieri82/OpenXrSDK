using System.Numerics;

namespace XrEngine
{
    public class StandardVertexShaderHandler : IShaderHandler
    {
        readonly Dictionary<uint, string> _lightHashes = [];

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ctx.Camera!;

                up.SetUniform("uViewProj", camera.ViewProjection);
                up.SetUniform("uViewPos", camera.WorldPosition, true);
                up.SetUniform("uFarPlane", camera.Far);

                if (ctx.ShadowMapProvider != null && ctx.ShadowMapProvider.Options.Mode != ShadowMapMode.None)
                {
                    up.SetUniform("uShadowMap", ctx.ShadowMapProvider.ShadowMap!, 14);
                    up.SetUniform("uLightSpaceMatrix", ctx.ShadowMapProvider.LightCamera!.ViewProjection);
                    if (ctx.ShadowMapProvider.Light != null)
                        up.SetUniform("uLightDirection", ctx.ShadowMapProvider.Light!.Direction);
                }

                if (ctx.Shader!.IsLit)
                {
                    foreach (var light in bld.Context.Lights!)
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

                    _lightHashes[ctx.ProgramInstanceId] = ctx.LightsHash!;
                }
            });
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return true;
        }

        public static readonly StandardVertexShaderHandler Instance = new();
    }
}

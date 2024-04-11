using System.Numerics;

namespace XrEngine
{
    public class StandardVertexShaderHandler : IShaderHandler
    {
        readonly Dictionary<uint, long> _lightVersions = [];

        public void UpdateShader(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ctx.Camera!;

                up.SetUniform("uView", camera.View);
                up.SetUniform("uProjection", camera.Projection);
                up.SetUniform("uViewPos", camera.Transform.Position, true);

                if (ctx.Shader!.IsLit && (!_lightVersions.TryGetValue(ctx.InstanceId, out var ver) || ctx.LightsVersion != ver))
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

                    _lightVersions[ctx.InstanceId] = ctx.LightsVersion;
                }
            });
        }

        public bool NeedUpdateShader(UpdateShaderContext ctx, ShaderUpdate lastUpdate)
        {
            return true;
        }

        public static readonly StandardVertexShaderHandler Instance = new();
    }
}

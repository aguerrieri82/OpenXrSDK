﻿using System.Numerics;
using System.Security.Cryptography;

namespace XrEngine
{
    public class StandardVertexShaderHandler : IShaderHandler
    {
        public virtual void UpdateShader(ShaderUpdateBuilder bld)
        {
            var shadowMode = bld.Context.ShadowMapProvider?.Options.Mode ?? ShadowMapMode.None;

            if (shadowMode != ShadowMapMode.None)
            {
                bld.AddFeature("USE_SHADOW_MAP");
                bld.AddFeature("SHADOW_MAP_MODE " + (int)shadowMode);

                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uShadowMap", ctx.ShadowMapProvider!.ShadowMap!, 14);
                    up.SetUniform("uLightSpaceMatrix", ctx.ShadowMapProvider.LightCamera!.ViewProjection);
                    if (ctx.ShadowMapProvider.Light != null)
                        up.SetUniform("uLightDirection", ctx.ShadowMapProvider.Light.Direction);
                });
            }


            bld.ExecuteAction((ctx, up) =>
            {
                var camera = ctx.Camera!;

                up.SetUniform("uViewProj", camera.ViewProjection);
                up.SetUniform("uViewPos", camera.WorldPosition, true);
                up.SetUniform("uFarPlane", camera.Far);
            });
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return false;
        }

        public static readonly StandardVertexShaderHandler Instance = new();
    }
}

﻿using System.Diagnostics;

namespace XrEngine
{
    public class StandardVertexShader : Shader, IShaderHandler
    {
        public StandardVertexShader()
        {
            VertexSourceName = "standard.vert";
            Resolver = str => Embedded.GetString(str);
        }

        public virtual void UpdateShader(ShaderUpdateBuilder bld)
        {
            var shadowMode = bld.Context.ShadowMapProvider?.Options.Mode ?? ShadowMapMode.None;

            if (shadowMode != ShadowMapMode.None)
            {
                bld.AddFeature("USE_SHADOW_MAP");
                bld.AddFeature("SHADOW_MAP_MODE " + (int)shadowMode);

                bld.ExecuteAction((ctx, up) =>
                {
                    up.LoadTexture(ctx.ShadowMapProvider!.ShadowMap!, 14);
                    if (ctx.ShadowMapProvider.Light != null)
                        up.SetUniform("uLightDirection", ctx.ShadowMapProvider.Light.Direction);
                });
            }

            bld.LoadBuffer((ctx) =>
            {
                Debug.Assert(ctx.PassCamera != null);

                var result = new CameraUniforms
                {
                    ViewProj = ctx.PassCamera.ViewProjection,
                    Position = ctx.PassCamera.WorldPosition,
                    Exposure = ctx.PassCamera.Exposure,
                    ActiveEye = ctx.PassCamera.ActiveEye,
                    ViewSize = ctx.PassCamera.ViewSize,
                    NearPlane = ctx.PassCamera.Near,
                    FarPlane = ctx.PassCamera.Far,
                    FrustumPlane1 = ctx.FrustumPlanes[0],
                    FrustumPlane2 = ctx.FrustumPlanes[1],
                    FrustumPlane3 = ctx.FrustumPlanes[2],
                    FrustumPlane4 = ctx.FrustumPlanes[3],
                    FrustumPlane5 = ctx.FrustumPlanes[4],
                    FrustumPlane6 = ctx.FrustumPlanes[5],
                    View = ctx.PassCamera.View,
                    Proj = ctx.PassCamera.Projection,
                };

                var light = ctx.ShadowMapProvider?.LightCamera?.ViewProjection;
                if (light != null)
                    result.LightSpaceMatrix = light.Value;

                return (CameraUniforms?)result;

            }, 0, BufferStore.Shader);
        }

        public virtual bool NeedUpdateShader(UpdateShaderContext ctx)
        {
            return false;
        }

        public static readonly StandardVertexShader Instance = new();
    }
}

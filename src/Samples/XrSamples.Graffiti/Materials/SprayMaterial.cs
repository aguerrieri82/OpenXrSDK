using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class SprayMaterial : ShaderMaterial
    {
        private Can? _can;

        public SprayMaterial()
        {
            Shader = new Shader
            {
                VertexSourceName = "can_spray.vert",
                FragmentSourceName = "can_spray.frag",
                Resolver = a => Embedded.GetString<SprayMaterial>(a)
            };

            SprayFarDistance = 1;
            RayLengthFalloff = 5f;
            DotLength = 0.001f;
            GapLength = 0.01f;
            DotSpeed = 0.3f;
            Alpha = AlphaMode.Blend;
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, bld) =>
            {
                bld.SetUniform("uViewProjection", ctx.MainCamera!.ViewProjection);
                bld.SetUniform("uSprayFarDistance", SprayFarDistance);

                bld.SetUniform("uRayLengthFalloff", RayLengthFalloff);

                bld.SetUniform("uDotLength", DotLength);
                bld.SetUniform("uGapLength", GapLength);
                bld.SetUniform("uDotSpeed", DotSpeed);
                bld.SetUniform("uTime", ctx.Time);
       
                _can ??= ctx.Model?.Scene?.Descendants<Can>().First();

                if (_can != null)
                    bld.SetUniform("uPaintColor", _can.Color.ToVector3());
            });

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

            base.UpdateShaderModel(bld);
        }

        public float SprayFarDistance { get; set; }

        public float RayLengthFalloff { get; set; }

        public float DotLength { get; set; }
        
        public float GapLength { get; set; }
      
        public float DotSpeed { get; set; }
        
        public Color PaintColor { get; set; }
    }
}

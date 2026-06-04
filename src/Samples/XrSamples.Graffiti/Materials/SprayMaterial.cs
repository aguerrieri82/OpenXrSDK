using System;
using System.Collections.Generic;
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

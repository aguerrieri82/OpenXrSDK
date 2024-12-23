﻿using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class GlowVolumeMaterial : ShaderMaterial
    {
        public static readonly Shader SHADER;

        static GlowVolumeMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "glow_vol.frag",
                IsLit = false
            };
        }


        public GlowVolumeMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            StepSize = 0.1f;
            Alpha = AlphaMode.Blend;
            UseDepth = false;
            WriteDepth = false;
        }

        public override void UpdateShader(ShaderUpdateBuilder bld)
        {

            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("sphereCenter", ctx.Model!.WorldPosition);
                up.SetUniform("sphereRadius", SphereRadius);
                up.SetUniform("uNormalMatrix", ctx.Model!.NormalMatrix);
                up.SetUniform("uModel", ctx.Model!.WorldMatrix);
                up.SetUniform("haloWidth", HaloWidth);
                up.SetUniform("haloColor", HaloColor);
                up.SetUniform("stepSize", StepSize);

            });
        }

        public float SphereRadius { get; set; }

        public float HaloWidth { get; set; }

        public Color HaloColor { get; set; }

        [Range(0, 1, 0.001f)]   
        public float StepSize { get; set; }
    }
}
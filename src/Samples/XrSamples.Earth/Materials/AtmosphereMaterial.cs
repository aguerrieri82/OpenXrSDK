﻿using System.Numerics;
using XrEngine;
using XrMath;

namespace XrSamples.Earth
{
    public class AtmosphereMaterial : GlowVolumeMaterial, IVolumeMaterial
    {
        public static readonly new Shader SHADER;


        static AtmosphereMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "atmosphere.frag",
                IsLit = false,
                SourcePaths = ["D:\\Development\\Personal\\Git\\XrSDK\\src\\Samples\\XrSamples.Earth\\Shaders\\"],
                Resolver = str =>
                {
                    if (str.EndsWith(".frag"))
                        return Embedded.GetString(str);
                    return Embedded.GetString<Material>(str);
                }
            };
        }


        public AtmosphereMaterial()
            : base()
        {
            _shader = SHADER;

            DoubleSided = true;
            StepSize = 0.1f;
            Alpha = AlphaMode.Blend;
            UseDepth = false;
            WriteDepth = false;
            SunIntensity = 22;
            SunColor = new Color(1.0f, 0.8f, 0.6f);
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.ExecuteAction((ctx, up) =>
            {
                up.SetUniform("uSunPosition", SunPosition);
                up.SetUniform("uSunIntensity", SunIntensity);
                up.SetUniform("uSunColor", (Vector3)SunColor);
            });

            base.UpdateShaderMaterial(bld);
        }


        [Range(1, 10, 0.1f)]
        public float SunIntensity { get; set; }

        public Vector3 SunPosition { get; set; }



        public Color SunColor { get; set; }
    }
}


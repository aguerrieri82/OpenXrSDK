using Silk.NET.Direct3D11;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CanMaterial : PbrV2Material
    {
        public CanMaterial()
        {

            FragmentDefaultLoader = $"LoadFragmentPropertiesCanColor(uCanColor.rgb)";

            FragmentDefaultShader = Embedded.GetString("PbrV2/pbr_defaults.glsl") +
                                    Embedded.GetString<Can>("can_pbr.glsl");

        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.SetUniform("uCanColor", (_) => CanColor);
            base.UpdateShaderMaterial(bld);
        }


        public Color CanColor { get; set; } 
    }
}

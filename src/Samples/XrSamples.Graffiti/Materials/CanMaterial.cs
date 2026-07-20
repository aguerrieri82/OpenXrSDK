using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CanMaterial : PbrMaterial
    {
        public CanMaterial()
        {

            FragmentDefaultLoader = $"LoadFragmentPropertiesCanColor(uCanColor.rgb)";

            FragmentDefaultShader = Embedded.GetString("Pbr/pbr_defaults.glsl") +
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

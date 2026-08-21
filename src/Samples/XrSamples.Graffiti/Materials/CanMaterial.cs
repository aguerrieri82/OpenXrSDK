using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CanMaterial : PbrMaterial
    {

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            base.UpdateShaderMaterial(bld);

            bld.SetFsIncludes("[XrSamples.Graffiti]can_pbr.glsl");
            bld.SetSlot(ShaderSlots.FragmentLoader, 
                () => "FragmentProperties frag = LoadFragmentPropertiesCanColor(uCanColor.rgb);");

            bld.SetUniform("uCanColor", (_) => CanColor);
        }

        public Color CanColor { get; set; }
    }
}

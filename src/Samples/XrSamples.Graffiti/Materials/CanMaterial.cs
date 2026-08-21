using XrEngine;
using XrMath;

namespace XrSamples.Graffiti
{
    public class CanMaterial : PbrMaterial
    {

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            base.UpdateShaderMaterial(bld);

            bld.SetSlot("FS_INCLUDES", () => "#include \"[XrSamples.Graffiti]can_pbr.glsl\"");
            bld.SetSlot("FRAGMENT_LOADER", () => "FragmentProperties frag = LoadFragmentPropertiesCanColor(uCanColor.rgb);");

            bld.SetUniform("uCanColor", (_) => CanColor);
        }

        public Color CanColor { get; set; }
    }
}

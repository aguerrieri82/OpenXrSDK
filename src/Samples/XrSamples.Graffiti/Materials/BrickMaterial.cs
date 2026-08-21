using XrEngine;
using XrMath;
using XrSamples.Graffiti.Objects;

namespace XrSamples.Graffiti
{
    public class BrickMaterial : PbrMaterial
    {

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            base.UpdateShaderMaterial(bld);

            bld.SetSlot("FS_INCLUDES", () => "#include \"[XrSamples.Graffiti]brick_pbr.glsl\"");
            bld.SetSlot("FRAGMENT_LOADER", () => "FragmentProperties frag = LoadFragmentPropertiesBrick();");
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.LoadBuffer<BrickUniforms>(ctx =>
            {
                var geo = ((TriangleMesh)ctx.Model!).Geometry as BrickGeometry;
                if (geo == null)
                    return null;
                return BrickUniforms.CreateDefault(geo);
            }, 15, BufferStore.Material);

            base.UpdateShaderModel(bld);
        }

        public Color CanColor { get; set; }
    }
}

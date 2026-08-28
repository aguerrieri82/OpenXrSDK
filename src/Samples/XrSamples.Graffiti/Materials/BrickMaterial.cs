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

            bld.SetFsIncludes("[XrSamples.Graffiti]brick_pbr.glsl");
            bld.SetFragmentLoader("frag = loadFragmentPropertiesBrick();");
        }

        protected override void UpdateShaderModel(ShaderUpdateBuilder bld)
        {
            bld.LoadBuffer<BrickUniforms>((ctx, ref update) =>
            {
                if (((TriangleMesh)ctx.Model!).Geometry is not BrickGeometry geo)
                    return false;

                update.Value = BrickUniforms.CreateDefault(geo);
                return true;

            }, 15, BufferStore.Material);

            base.UpdateShaderModel(bld);
        }

        public Color CanColor { get; set; }
    }
}

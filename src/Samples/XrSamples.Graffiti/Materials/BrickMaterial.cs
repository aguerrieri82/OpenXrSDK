using XrEngine;
using XrMath;
using XrSamples.Graffiti.Objects;

namespace XrSamples.Graffiti
{
    public class BrickMaterial : PbrMaterial
    {
        public BrickMaterial()
        {

            FragmentDefaultLoader = $"LoadFragmentPropertiesBrick()";

            FragmentDefaultShader = Embedded.GetString("Pbr/pbr_defaults.glsl") +
                                    Embedded.GetString<BrickMaterial>("brick_pbr.glsl");

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

using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Reconstruct
{
    public class MultiTextureMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static MultiTextureMaterial()
        {
            SHADER = new StandardVertexShader()
            {
                FragmentSourceName = "[XrEngine.Reconstruct]multi_tex.frag",
            };
        }

        public MultiTextureMaterial()
        {
            _shader = SHADER;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("TANGENT_AS_CONST");
            bld.AddFeature("HAS_TANGENTS");
            bld.AddFeature("HAS_UV2");

            bld.ExecuteAction((ctx, up) =>
            {
                up.LoadTexture(Texture!, 1);
            });

            base.UpdateShaderMaterial(bld);
        }

        public Texture2D? Texture { get; set; }
    }
}

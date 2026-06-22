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
            Exposure = 1;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            bld.AddFeature("TANGENT_AS_CONST");
            bld.AddFeature("HAS_TANGENTS");
            bld.AddFeature("HAS_UV2");

            bld.ExecuteAction((ctx, up) =>
            {
                up.LoadTexture(Texture!, 1);
                up.SetUniform("uExposure", Exposure);
            });

            base.UpdateShaderMaterial(bld);
        }


        [Range(0,1, 0.01f)]
        public float Exposure { get; set; }

        public Texture2D? Texture { get; set; }
    }
}

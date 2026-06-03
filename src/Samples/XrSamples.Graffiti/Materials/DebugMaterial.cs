using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;

namespace XrSamples.Graffiti
{
    public class DebugMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DebugMaterial()
        {
            SHADER = new StandardVertexShader
            {
                FragmentSourceName = "texture.frag",
                IsLit = false,
                Resolver = str => str == "texture.frag" ?
                    Embedded.GetString<DebugMaterial>(str) :
                    Embedded.GetString<StandardVertexShader>(str)
            };
        }

        public DebugMaterial()
            : base()
        {
            _shader = SHADER;
        }


        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            if (Texture?.Depth > 1)
            {
                bld.AddFeature("ARRAY");
                bld.ExecuteAction((ctx, up) =>
                {
                    up.SetUniform("uIndex", Index);
                });
            }

            bld.ExecuteAction((ctx, up) =>
            {
                if (Texture != null)
                    up.LoadTexture(Texture, 1);
            });
        }

        public Texture2D? Texture { get; set; }

        public int Index { get; set; }
    }
}

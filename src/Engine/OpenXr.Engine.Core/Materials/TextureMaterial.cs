using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class TextureMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static TextureMaterial()
        {
            SHADER = new Shader
            {
                FragmentSource = Embedded.GetString("texture_fs.glsl"),
                VertexSource = Embedded.GetString("standard_vs.glsl"),
                IsLit = false
            };
        }


        public TextureMaterial()
            : base()    
        {
            _shader = SHADER;
        }

        public TextureMaterial(Texture2D texture)
            : this()
        {
            Texture = texture;
        }


        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("uTexture0", Texture!, 0);
        }

        public Texture2D? Texture { get; set; }
    }
}

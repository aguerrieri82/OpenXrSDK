using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class DepthOnlyMaterial : ShaderMaterial
    {
        static readonly Shader SHADER;

        static DepthOnlyMaterial()
        {
            SHADER = new Shader
            {
                FragmentSource = Embedded.GetString("color_fs.glsl"),
                VertexSource = Embedded.GetString("standard_vs.glsl"),
                IsLit = false
            };
        }


        public DepthOnlyMaterial()
            : base()    
        {
            _shader = SHADER;
            WriteColor = false;
        }

        public override void UpdateUniforms(IUniformProvider obj)
        {
            obj.SetUniform("color", Color.Transparent);
        }

        public static readonly DepthOnlyMaterial Instance = new DepthOnlyMaterial();    
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class ShaderMaterial : Material
    {
        
        public virtual void UpdateUniforms(IUniformProvider obj)
        {

        }

        public Shader? Shader { get; set; }
    }
}

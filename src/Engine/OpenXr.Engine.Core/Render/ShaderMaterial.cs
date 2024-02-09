using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class ShaderMaterial : Material
    {
        protected Shader? _shader;

        public ShaderMaterial()
        {
        }

        public ShaderMaterial(Shader shader)
        {
            _shader = shader;
        }

        public virtual void UpdateUniforms(IUniformProvider obj)
        {

        }

        public Shader? Shader
        {
            get => _shader;
            set
            {
                if (value == _shader)
                    return;
                _shader = value;
                NotifyChanged();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class UpdateShaderContext
    {
        public Camera? Camera { get; set; }

        public IEnumerable<Light>? Lights { get; set; }

        public Object3D? Model { get; set; }

        public IRenderEngine? RenderEngine { get; set; }

        public VertexComponent ActiveComponents { get; set; }
    }

    public delegate void UpdateUniformAction(IUniformProvider uniformProvider);



    public interface IShaderHandler
    {
        void UpdateShader(UpdateShaderContext ctx, IUniformProvider up, IFeatureList fl);
    }
}

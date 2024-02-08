using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Mesh : Object3D
    {
        public Mesh()
        {
        }

        public Mesh(Geometry geometry, Material material)
        {
            Geometry = geometry;
            Materials = [material];
        }

        public override void Update(RenderContext ctx)
        {
            Geometry?.Update(ctx);

            Materials?.Update(ctx);  

            base.Update(ctx);
        }

        public IList<Material>? Materials { get; set; }  

        public Geometry? Geometry { get; set; }  
    }
}

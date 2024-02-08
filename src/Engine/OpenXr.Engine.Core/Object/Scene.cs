using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Scene : Group
    {
        public Scene()
        {
        }

        public void Render(RenderContext ctx)
        {
            Update(ctx);

        }

        public Camera? ActiveCamera { get; set; }   
    }
}

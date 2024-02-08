using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface IBehavior : IComponent, IRenderUpdate
    {

        void Start(RenderContext ctx);

    }
}

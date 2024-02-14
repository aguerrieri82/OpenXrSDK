using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenXr
{
    public interface IRayTarget : IComponent
    {
        void NotifyCollision(RenderContext ctx, Collision collision);
    }
}

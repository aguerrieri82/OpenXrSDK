using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xr.Engine;
using Xr.Engine.Filament;

namespace Xr.Editor.Components
{
    public class FlRenderHost : RenderHost
    {
        public override IRenderEngine CreateRenderEngine()
        {
            var render = new FilamentRender(new FilamentOptions
            {
                HWnd = HWnd,
                MaterialCachePath = "d:\\Materials"
            });

            return render;
        }
    }
}

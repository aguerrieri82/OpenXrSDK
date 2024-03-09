using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.OpenXr;

namespace XrEditor
{
    public interface IRenderSurfaceProvider
    {
        IRenderSurface CreateRenderSurface(GraphicDriver driver);

        IRenderSurface RenderSurface { get; }
    }
}

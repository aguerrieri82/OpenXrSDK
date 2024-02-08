using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{

    public unsafe interface IXrGraphicDriver : IXrPlugin
    {
        GraphicsBinding CreateBinding();

        long SelectSwapChainFormat(IList<long> availFormats);

        XrDynamicType SwapChainImageType { get; }    
    }
}

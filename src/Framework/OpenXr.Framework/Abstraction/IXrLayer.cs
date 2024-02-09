using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public unsafe interface IXrLayer : IDisposable
    {
        bool Render(ref View[] views, XrSwapchainInfo[] swapchains, long predTime);

        bool IsEnabled { get; set; }

        CompositionLayerBaseHeader* Header { get; }
    }
}

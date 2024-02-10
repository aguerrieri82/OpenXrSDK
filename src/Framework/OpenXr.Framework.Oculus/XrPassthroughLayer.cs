using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Oculus
{
    public class XrPassthroughLayer : BaseXrLayer<CompositionLayerPassthroughFB>
    {
        public XrPassthroughLayer()
        {
        }

        protected override bool Render(ref CompositionLayerPassthroughFB layer, ref View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            throw new NotImplementedException();
        }
    }
}

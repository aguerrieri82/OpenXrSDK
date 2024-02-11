using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using Silk.NET.OpenXR;
using Silk.NET.OpenXR.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test
{
    public class SampleXrApp : XrApp
    {
        public SampleXrApp(params IXrPlugin[] plugins)
           : base(NullLogger<XrApp>.Instance, plugins)
        {

        }

        protected override void SelectRenderOptionsMode(XrViewInfo viewInfo, XrRenderOptions result)
        {
            base.SelectRenderOptionsMode(viewInfo, result);

            result.SampleCount = 2;

            result.Size = new Extent2Di
            {
                Height = result.Size.Height / 2,
                Width = result.Size.Width / 2,
            };

        }

    }
}

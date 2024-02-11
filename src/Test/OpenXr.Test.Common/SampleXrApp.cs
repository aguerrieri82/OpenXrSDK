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

            var scaleFactor = 1f;

            result.Size = new Extent2Di
            {
                Height = (int)(result.Size.Height * scaleFactor),
                Width = (int)(result.Size.Width * scaleFactor),
            };

        }

    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using Silk.NET.OpenXR;

namespace OpenXr.Test
{
    public class SampleXrApp : XrApp
    {
        public SampleXrApp(ILogger logger, params IXrPlugin[] plugins)
           : base(logger, plugins)
        {

        }

        protected override void SelectRenderOptions(XrViewInfo viewInfo, XrRenderOptions result)
        {
            base.SelectRenderOptions(viewInfo, result);

            var scaleFactor = 1.4f;

            result.Size = new Extent2Di
            {
                Height = (int)(result.Size.Height * scaleFactor),
                Width = (int)(result.Size.Width * scaleFactor),
            };

        }

    }
}

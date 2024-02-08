using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class XrRenderOptions
    {
        public Extent2Di Size { get; set; }

        public EnvironmentBlendMode BlendMode { get; set; } 

        public uint SampleCount { get; set; }


    }
}

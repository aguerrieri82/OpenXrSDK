using Silk.NET.Core.Attributes;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public class XrViewInfo
    {
        public ViewConfigurationType Type { get; set; } 

        public bool FovMutable { get; set; }    

        public Extent2Di RecommendedImageRect { get; set; }

        public Extent2Di MaxImageRect { get; set; }

        public uint RecommendedSwapchainSampleCount { get; set; }

        public uint MaxSwapchainSampleCount { get; set; }

        public int ViewCount { get; set; }

        public EnvironmentBlendMode[]? BlendModes { get; internal set; }
    }
}

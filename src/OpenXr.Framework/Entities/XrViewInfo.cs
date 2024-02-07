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

        public SizeI RecommendedImageRect { get; set; }

        public SizeI MaxImageRect { get; set; }

        public uint RecommendedSwapchainSampleCount { get; set; }

        public uint MaxSwapchainSampleCount { get; set; }

    }
}

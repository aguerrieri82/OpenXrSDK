using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Framework.Oculus
{
    public class METARecommendedLayerResolution : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_recommended_layer_resolution";

        public METARecommendedLayerResolution(XR xr, Instance instance) : base(xr, instance)
        {
        }

        [AllowNull]
        public GetRecommendedLayerResolutionMETADelegate GetRecommendedLayerResolutionMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetRecommendedLayerResolutionMETADelegate(Session session, ref RecommendedLayerResolutionGetInfoMETA info, ref RecommendedLayerResolutionMETA resolution);
    }

}

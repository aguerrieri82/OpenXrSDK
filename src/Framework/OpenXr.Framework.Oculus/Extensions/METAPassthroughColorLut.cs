using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public class METAPassthroughColorLut : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_passthrough_color_lut";

        public METAPassthroughColorLut(XR xr, Instance instance) : base(xr, instance)
        {
        }

        [AllowNull]
        public CreatePassthroughColorLutMETADelegate CreatePassthroughColorLutMETA;

        [AllowNull]
        public UpdatePassthroughColorLutMETADelegate UpdatePassthroughColorLutMETA;

        [AllowNull]
        public DestroyPassthroughColorLutMETADelegate DestroyPassthroughColorLutMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreatePassthroughColorLutMETADelegate(PassthroughFB passthrough, ref PassthroughColorLutCreateInfoMETA createInfo, ref PassthroughColorLutMETA colorLut);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result UpdatePassthroughColorLutMETADelegate(PassthroughColorLutMETA colorLut, ref PassthroughColorLutUpdateInfoMETA updateInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DestroyPassthroughColorLutMETADelegate(PassthroughColorLutMETA colorLut);
    }
}
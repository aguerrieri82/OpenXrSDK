using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenXr.Framework.Oculus
{
    public class METAPassthroughPreferences : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_passthrough_preferences";

        public METAPassthroughPreferences(XR xr, Instance instance)
            : base(xr, instance)
        {
        }

        [AllowNull]
        public GetPassthroughPreferencesMETADelegate GetPassthroughPreferencesMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetPassthroughPreferencesMETADelegate(
            Session session,
            ref PassthroughPreferencesMETA preferences);
    }
}

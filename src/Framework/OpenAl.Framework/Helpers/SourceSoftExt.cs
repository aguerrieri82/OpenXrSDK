using Silk.NET.OpenAL;
using System.Runtime.InteropServices;

namespace OpenAl.Framework.Helpers
{
    public static class SourceSoftExt
    {
        public const int AL_SAMPLE_OFFSET_LATENCY_SOFT = 0x1200;
        public const int AL_SEC_OFFSET_LATENCY_SOFT = 0x1201;
        public const int ALC_DEVICE_CLOCK_LATENCY_SOFT = 0x1602;


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void alGetSourcei64vSOFTDelegate(uint source, int param, long* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void alcGetInteger64vSOFTDelegate(IntPtr device, int param, int size, out long values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void alGetSourcedvSOFTDelegate(uint source, int param, double* values);

        public static unsafe void Init(ALContext ctx, Device* device)
        {
            GetInteger64 = Marshal.GetDelegateForFunctionPointer<alcGetInteger64vSOFTDelegate>((nint)ctx.GetProcAddress(device, "alcGetInteger64vSOFT"));

            GetSourceInteger64 = Marshal.GetDelegateForFunctionPointer<alGetSourcei64vSOFTDelegate>((nint)ctx.GetProcAddress(device, "alcGetInteger64vSOFT"));

            GetSourceDouble = Marshal.GetDelegateForFunctionPointer<alGetSourcedvSOFTDelegate>((nint)ctx.GetProcAddress(device, "alGetSourcedvSOFT"));

        }

        public static alcGetInteger64vSOFTDelegate GetInteger64;

        public static alGetSourcei64vSOFTDelegate GetSourceInteger64;

        public static alGetSourcedvSOFTDelegate GetSourceDouble;
    }
}

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
        unsafe delegate void alGetSourcei64vSOFTDelegate(uint source, int param, long* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void alcGetInteger64vSOFTDelegate(Device* device, int param, int size, long* values);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void alGetSourcedvSOFTDelegate(uint source, int param, double* values);

        static alcGetInteger64vSOFTDelegate _getInteger64 = null!;
        static alGetSourcei64vSOFTDelegate _getSourceInteger64 = null!;
        static alGetSourcedvSOFTDelegate _getSourceDouble = null!;

        public static unsafe void Init(ALContext ctx, Device* device)
        {
            _getInteger64 = Marshal.GetDelegateForFunctionPointer<alcGetInteger64vSOFTDelegate>((nint)ctx.GetProcAddress(device, "alcGetInteger64vSOFT"));
            _getSourceInteger64 = Marshal.GetDelegateForFunctionPointer<alGetSourcei64vSOFTDelegate>((nint)ctx.GetProcAddress(device, "alGetSourcei64vSOFT"));
            _getSourceDouble = Marshal.GetDelegateForFunctionPointer<alGetSourcedvSOFTDelegate>((nint)ctx.GetProcAddress(device, "alGetSourcedvSOFT"));
        }

        public static unsafe void GetInteger64(Device* device, int param, out long value)
        {
            long result;
            _getInteger64(device, param, 1, &result);
            value = result;
        }

        public static unsafe void GetInteger64(Device* device, int param, Span<long> values)
        {
            if (values.IsEmpty)
                return;

            fixed (long* ptr = values)
                _getInteger64(device, param, values.Length, ptr);
        }

        public static unsafe void GetSourceInteger64(uint source, int param, out long value)
        {
            long result;
            _getSourceInteger64(source, param, &result);
            value = result;
        }

        public static unsafe void GetSourceInteger64(uint source, int param, Span<long> values)
        {
            if (values.IsEmpty)
                return;

            fixed (long* ptr = values)
                _getSourceInteger64(source, param, ptr);
        }

        public static unsafe void GetSourceDouble(uint source, int param, out double value)
        {
            double result;
            _getSourceDouble(source, param, &result);
            value = result;
        }

        public static unsafe void GetSourceDouble(uint source, int param, Span<double> values)
        {
            if (values.IsEmpty)
                return;

            fixed (double* ptr = values)
                _getSourceDouble(source, param, ptr);
        }
    }
}
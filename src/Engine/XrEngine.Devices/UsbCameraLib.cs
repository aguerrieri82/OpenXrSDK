using System.Numerics;
using System.Runtime.InteropServices;

namespace XrEngine.Devices
{
    public static class UsbCameraLib
    {
        const string Dll = "usbcamera-native";

        public enum UvcFrameFormat
        {
            Unknown = 0,
            Yuyv = 3,
            Mjpeg = 7,
            H264 = 8,
            Nv12 = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct CameraHandle
        {
            internal readonly nint Handle;

            public CameraHandle(nint handle)
            {
                Handle = handle;
            }

            public bool IsNull => Handle == 0;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DeviceInfo
        {
            public int Index;

            public ushort VendorId;
            public ushort ProductId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Manufacturer;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Product;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string Serial;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FormatInfo
        {
            public int Index;

            public UvcFrameFormat FrameFormat;
            public byte DescriptorSubtype;
            public byte FormatIndex;
            public byte FrameIndex;

            public int Width;
            public int Height;

            public int FpsCount;

            public int DefaultFps;
            public int MinFps;
            public int MaxFps;
            public int StepFps;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FrameInfo
        {
            public int Width;
            public int Height;

            public UvcFrameFormat FrameFormat;

            public uint Sequence;

            public IntPtr Data;
            public int DataBytes;
        }


        [DllImport(Dll)]
        public static extern CameraHandle Create();

        [DllImport(Dll)]
        public static extern void Destroy(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int Init(this CameraHandle camera, bool noDeviceDiscovery = false, bool enableDebug = false);

        [DllImport(Dll)]
        public static extern void Shutdown(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int RefreshDevices(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int GetDeviceCount(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int GetDeviceInfo(this CameraHandle camera, int deviceIndex, out DeviceInfo outInfo);

        [DllImport(Dll)]
        public static extern int OpenDevice(this CameraHandle camera, int deviceIndex);

        [DllImport(Dll)]
        public static extern int OpenDeviceFd(this CameraHandle camera, int fd, int vendorId, int productId);

        [DllImport(Dll)]
        public static extern void CloseDevice(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int RefreshFormats(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int GetFormatCount(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int GetFormatInfo(this CameraHandle camera, int formatIndex, out FormatInfo outInfo);

        [DllImport(Dll)]
        public static extern int OpenStreamByFormatIndex(this CameraHandle camera, int formatIndex, uint fps);

        [DllImport(Dll)]
        public static extern int OpenStream(this CameraHandle camera, UvcFrameFormat frameFormat, int width, int height, uint fps);

        [DllImport(Dll)]
        public static extern int StartStream(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern void StopStream(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern void CloseStream(this CameraHandle camera);

        [DllImport(Dll)]
        public static extern int PullFrame(this CameraHandle camera, int timeoutMs, out FrameInfo outFrame);

        [DllImport(Dll)]
        public static extern int CopyFrame(this CameraHandle camera, nint dst, int dstBytes, out int outBytesWritten);

        [DllImport(Dll)]
        private static extern nint GetLastErrorText(CameraHandle camera);

        public static string GetLastError(this CameraHandle camera)
        {
            return Marshal.PtrToStringAnsi(GetLastErrorText(camera)) ?? "";
        }

        public static void Check(this CameraHandle camera, int result)
        {
            if (result < 0)
                throw new InvalidOperationException(camera.GetLastError());
        }
    }
}

using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Enumeration;
using System.Runtime.InteropServices;

namespace OpenAl.Framework
{
    public enum GetDeviceInt64
    {
        Clock = 0x1600,
        Latency = 0x1601,
        ClockLatency = 0x1602
    }

    unsafe delegate void alcGetInteger64vSOFTDelegate(Device* device, int pname, uint size, long* values);

    public unsafe class AlDevice
    {
        const int ALC_ALL_DEVICES_SPECIFIER = 0x1013;

        readonly alcGetInteger64vSOFTDelegate GetInteger64;

        private Device* _device;
        private Context* _context;
        private readonly AL _al;
        private static readonly ALContext _alc;

        static AlDevice()
        {
            _alc = ALContext.GetApi();
        }

        public AlDevice(string? deviceName = null)
        {
            _al = AL.GetApi();

            CreateContext(deviceName);

            GetInteger64 = Marshal.GetDelegateForFunctionPointer<alcGetInteger64vSOFTDelegate>((nint)_alc.GetProcAddress(_device, "alcGetInteger64vSOFT"));

            Current = this;
        }

        public ulong Latency
        {
            get
            {
                long result;
                GetInteger64(_device, (int)GetDeviceInt64.Latency, 1, &result);
                return (ulong)result;
            }
        }

        public ulong Clock
        {
            get
            {
                long result;
                GetInteger64(_device, (int)GetDeviceInt64.Clock, 1, &result);
                return (ulong)result;
            }
        }

        protected void CreateContext(string? deviceName = null)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                deviceName = ListDevices(true).First();

            _device = _alc.OpenDevice(deviceName);

            int[] attrs = [0];

            fixed (int* ptr = &attrs[0])
                _context = _alc.CreateContext(_device, ptr);

            _alc.MakeContextCurrent(_context);
        }


        public static IList<string> ListDevices(bool onlyDefault)
        {
            var result = new List<string>();

            if (_alc.TryGetExtension<Enumeration>(null, out var enumeration))
            {
                var devType = onlyDefault ? GetEnumerationContextStringList.DeviceSpecifiers :
                              (GetEnumerationContextStringList)ALC_ALL_DEVICES_SPECIFIER;

                result.AddRange(enumeration.GetStringList(devType));
            }

            return result;
        }

        public AL Al => _al;

        public static AlDevice? Current { get; internal set; }
    }
}

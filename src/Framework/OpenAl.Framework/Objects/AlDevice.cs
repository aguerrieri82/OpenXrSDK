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

    public unsafe class AlDevice
    {
        delegate void alcGetInteger64vSOFTDelegate(Device* device, int pname, uint size, long* values);

        alcGetInteger64vSOFTDelegate alcGetInteger64vSOFT;

        private Device* _device;
        private Context* _context;
        private readonly AL _al;
        private readonly ALContext _alc;

        public AlDevice()
        {
            _alc = ALContext.GetApi();
            _al = AL.GetApi();
            
            CreateContext();

            alcGetInteger64vSOFT = Marshal.GetDelegateForFunctionPointer<alcGetInteger64vSOFTDelegate>((nint)_alc.GetProcAddress(_device, "alcGetInteger64vSOFT"));

            Current = this;
        }

        public ulong Latency
        {
            get
            {
                long result;
                alcGetInteger64vSOFT(_device, (int)GetDeviceInt64.Latency, 1, &result);
                return (ulong)result;
            }
        }

        public ulong Clock
        {
            get
            {
                long result;
                alcGetInteger64vSOFT(_device, (int)GetDeviceInt64.Clock, 1, &result);
                return (ulong)result;
            }
        }

        protected void CreateContext()
        {
            var devices = ListDevices();

            _device = _alc.OpenDevice(devices[0]);
            int[] attrs = [0];

            fixed (int* ptr = &attrs[0])
                _context = _alc.CreateContext(_device, ptr);

            _alc.MakeContextCurrent(_context);
        }


        public IList<string> ListDevices()
        {
            var result = new List<string>();

            if (_alc.TryGetExtension<Enumeration>(null, out var enumeration))
            {
                foreach (var device in enumeration.GetStringList(GetEnumerationContextStringList.DeviceSpecifiers))
                    result.Add(device);
            }

            return result;
        }

        public AL Al => _al;

        public static AlDevice? Current { get; internal set; }
    }
}

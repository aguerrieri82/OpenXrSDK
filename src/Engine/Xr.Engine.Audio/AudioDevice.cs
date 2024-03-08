using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Enumeration;

namespace Xr.Engine.Audio
{
    public unsafe class AudioDevice
    {
        private Device* _device;
        private Context* _context;
        private readonly AL _al;

        public AudioDevice()
        {
            CreateContext();
            _al = AL.GetApi();
        }

        protected void CreateContext()
        {
            using var alc = ALContext.GetApi();

            var devices = ListDevices(alc);

            _device = alc.OpenDevice(devices[0]);
            int[] attrs = [0];

            fixed (int* ptr = &attrs[0])
                _context = alc.CreateContext(_device, ptr);

            alc.MakeContextCurrent(_context);
        }


        public IList<string> ListDevices(ALContext alc)
        {
            var result = new List<string>();

            if (alc.TryGetExtension<Enumeration>(null, out var enumeration))
            {
                foreach (var device in enumeration.GetStringList(GetEnumerationContextStringList.DeviceSpecifiers))
                    result.Add(device);
            }

            return result;
        }
    }
}

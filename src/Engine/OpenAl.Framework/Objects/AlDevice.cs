using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Enumeration;

namespace OpenAl.Framework
{
    public unsafe class AlDevice
    {
        private Device* _device;
        private Context* _context;
        private readonly AL _al;
        private readonly ALContext _alc;

        public AlDevice()
        {
            _alc = ALContext.GetApi();
            _al = AL.GetApi();

            CreateContext();
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
    }
}

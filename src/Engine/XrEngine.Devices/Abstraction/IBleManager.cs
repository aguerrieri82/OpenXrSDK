using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Devices
{
    public class BleDeviceInfo
    {
        public BleAddress Address { get; set; }

        public string? Name { get; set; }
    }

    public class BleDeviceFilter
    {
        public string? Name { get; set; }

        public int MaxDevices { get; set; }

        public TimeSpan Timeout { get; set; }
    }

    public struct BleAddress
    {
        public ulong Value;   

        public static implicit operator BleAddress(ulong value) => new() { Value = value };    
    }

    public interface IBleManager
    {
        Task<IList<BleDeviceInfo>> FindDevicesAsync(BleDeviceFilter filter);

        Task<IBleDevice> GetDeviceAsync(BleAddress address);
    }
}

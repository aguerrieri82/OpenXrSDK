using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Devices
{
    public class BleDeviceInfo
    {
        public BleAddress Address;

        public string? Name;
    }

    public class BleDeviceFilter
    {
        public string? Name;

        public int MaxDevices;

        public TimeSpan Timeout;    
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

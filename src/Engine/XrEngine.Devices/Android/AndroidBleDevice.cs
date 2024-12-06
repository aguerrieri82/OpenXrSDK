#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Devices.Android
{
    public class AndroidBleDevice : IBleDevice
    {
        public bool IsConnected => throw new NotImplementedException();

        public void AddCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler)
        {
            throw new NotImplementedException();
        }

        public Task ConnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task DisconnectAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BleCharacteristicInfo>> GetCharacteristicsAsync(BleServiceInfo serviceInfo, int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<BleServiceInfo>> GetServicesAsync(int timeoutMs)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ReadCharacteristicAsync(BleCharacteristicInfo characteristicInfo)
        {
            throw new NotImplementedException();
        }

        public void RemoveCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler)
        {
            throw new NotImplementedException();
        }

        public Task WriteCharacteristic(BleCharacteristicInfo characteristicInfo, byte[] data)
        {
            throw new NotImplementedException();
        }

        public Task WriteCharacteristicConfigurationAsync(BleCharacteristicInfo characteristicInfo, BleCharacteristicConfig value)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
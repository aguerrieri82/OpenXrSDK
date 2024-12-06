using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Devices
{
    public class BleServiceInfo
    {
        public Guid Id { get; set; }
    }

    public class BleCharacteristicInfo
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public BleServiceInfo? Service { get; set; }
    }

    public enum BleCharacteristicConfig
    {
        None,
        Notify,
        Indicate
    }

    public delegate void BleCharacteristicValueChangedDelegate(BleCharacteristicInfo characteristicInfo, byte[] value); 


    public interface IBleDevice
    {
        Task ConnectAsync();

        Task DisconnectAsync();

        Task< IEnumerable<BleServiceInfo>> GetServicesAsync(int timeoutMs);

        Task<IEnumerable<BleCharacteristicInfo>> GetCharacteristicsAsync(BleServiceInfo serviceInfo, int timeoutMs);   

        Task<byte[]> ReadCharacteristicAsync(BleCharacteristicInfo characteristicInfo);

        Task WriteCharacteristic(BleCharacteristicInfo characteristicInfo, byte[] data);

        Task WriteCharacteristicConfigurationAsync(BleCharacteristicInfo characteristicInfo, BleCharacteristicConfig value);

        void AddCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler);

        void RemoveCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler);


        bool IsConnected { get; }   
    }
}

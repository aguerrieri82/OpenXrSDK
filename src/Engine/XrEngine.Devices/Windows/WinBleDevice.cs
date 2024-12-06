#if WINDOWS

using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Channels;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Security.Cryptography;

namespace XrEngine.Devices.Windows
{
    public class WinBleDevice : IBleDevice
    {
        BluetoothLEDevice? _device;
        GattDeviceServicesResult? _services;
        readonly Dictionary<Guid, GattCharacteristicsResult> _characteristics = [];
        readonly Dictionary<BleCharacteristicValueChangedDelegate, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs>> _valueChanged = [];

        private readonly BleAddress _address;

        public WinBleDevice(BleAddress address)
        {
            _address = address;
        }


        public async Task ConnectAsync()
        {
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(_address.Value);
            if (_device == null)
                throw new Exception("Device not found");    
        }

        public Task DisconnectAsync()
        {
            if (_device != null)
            {
                _device.Dispose();
                _device = null;
            }

            if (_services != null)
            {
                foreach (var service in _services.Services)
                    service.Dispose();
                _services = null;
            }

            _characteristics?.Clear();

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<BleCharacteristicInfo>> GetCharacteristicsAsync(BleServiceInfo serviceInfo, int timeoutMs)
        {
            var service = _services!.Services.Single(a => a.Uuid == serviceInfo.Id);

            if (!_characteristics.TryGetValue(serviceInfo.Id, out var chars))
            {
                var startTime = DateTime.UtcNow;

                while (true)
                {
                    try
                    {
                        chars = await service!.GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
                           .AsTask()
                           .WaitAsync(TimeSpan.FromSeconds(5));

                        if (chars.Status == GattCommunicationStatus.Success)
                        {
                            _characteristics[serviceInfo.Id] = chars;
                            break;
                        }
                    }
                    catch
                    {
                    }

                    if (timeoutMs > 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                        throw new TimeoutException();

                    await Task.Delay(200);
                }
            }

            return chars.Characteristics.Select(a => new BleCharacteristicInfo
            {
                Id = a.Uuid,
                Service = serviceInfo,
                Name = a.UserDescription
            });
        }

        public async Task<IEnumerable<BleServiceInfo>> GetServicesAsync(int timeoutMs)
        {
            if (_services == null)
            {
                var startTime = DateTime.UtcNow;

                while (true)
                {
                    try
                    {
                        _services = await _device!.GetGattServicesAsync(BluetoothCacheMode.Uncached)
                        .AsTask()
                        .WaitAsync(TimeSpan.FromSeconds(40));

                        if (_services.Status == GattCommunicationStatus.Success)
                            break;

                        Console.WriteLine(_services.Status);
                    }
                    catch
                    {
                        Console.WriteLine("Timeout");
                    }

                    if (timeoutMs > 0 && (DateTime.UtcNow - startTime).TotalMilliseconds > timeoutMs)
                        throw new TimeoutException();

                    await Task.Delay(200);
                }
            }

            return _services.Services.Select(a => new BleServiceInfo { Id = a.Uuid });
        }

        public async Task<byte[]> ReadCharacteristicAsync(BleCharacteristicInfo characteristicInfo)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);
            var buffer = await cts.ReadValueAsync(BluetoothCacheMode.Uncached);
            return buffer.Value.ToArray();
        }


        public async Task WriteCharacteristicAsync(BleCharacteristicInfo characteristicInfo, byte[] data)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);
            await cts.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(data));
        }

        public async Task WriteCharacteristicConfigurationAsync(BleCharacteristicInfo characteristicInfo, BleCharacteristicConfig value)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);
            await cts.WriteClientCharacteristicConfigurationDescriptorAsync((GattClientCharacteristicConfigurationDescriptorValue)value);
        }

        public void RemoveCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);
            var changed = _valueChanged[handler];
            cts.ValueChanged -= changed;
            _valueChanged.Remove(handler);
        }

        public void AddCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);

            _valueChanged[handler] = (sender, args) =>
            {
                handler(characteristicInfo, args.CharacteristicValue.ToArray());
            };

            cts.ValueChanged += _valueChanged[handler];
        }

        GattCharacteristic GetCharacteristicInternal(BleCharacteristicInfo characteristicInfo)
        {
            return _characteristics[characteristicInfo.Service!.Id].Characteristics.Single(a => a.Uuid == characteristicInfo.Id);
        }


        public bool IsConnected => _device != null && _device.ConnectionStatus == BluetoothConnectionStatus.Connected;
    }
}

#endif
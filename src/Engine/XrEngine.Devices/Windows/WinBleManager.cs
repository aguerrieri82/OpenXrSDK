#if WINDOWS


using Windows.Devices.Bluetooth.Advertisement;

namespace XrEngine.Devices.Windows
{
    public class WinBleManager : IBleManager
    {
        public async Task<IList<BleDeviceInfo>> FindDevicesAsync(BleDeviceFilter filter)
        {
            var result = new List<BleDeviceInfo>();

            var cancel = new CancellationTokenSource();

            var filter2 = new BluetoothLEAdvertisementFilter();

            var watcher = new BluetoothLEAdvertisementWatcher(filter2)
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            watcher.Received += (s, e) =>
            {
                Console.WriteLine($"{e.BluetoothAddress} - {e.Advertisement.LocalName}");

                if (!string.IsNullOrWhiteSpace(filter.Name) && e.Advertisement.LocalName != filter.Name)
                    return;


                var curDev = result.FirstOrDefault(a => a.Address.Value == e.BluetoothAddress);

                if (curDev == null)
                {
                    curDev = new BleDeviceInfo
                    {
                        Address = new BleAddress
                        {
                            Value = e.BluetoothAddress
                        }
                    };
                    result.Add(curDev);
                }

                if (!string.IsNullOrWhiteSpace(e.Advertisement.LocalName))
                    curDev.Name = e.Advertisement.LocalName;

                if (filter.MaxDevices > 0 && result.Count >= filter.MaxDevices)
                    cancel.Cancel();
            };

            watcher.Start();

            try
            {
                await Task.Delay(filter.Timeout, cancel.Token);
            }
            catch
            {

            }

            watcher.Stop();

            return result;
        }

        public Task<IBleDevice> GetDeviceAsync(BleAddress address)
        {
            return Task.FromResult<IBleDevice>(new WinBleDevice(address));
        }
    }
}

#endif
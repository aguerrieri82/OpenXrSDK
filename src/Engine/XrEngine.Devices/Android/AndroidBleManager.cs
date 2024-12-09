#if __ANDROID__

using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;

namespace XrEngine.Devices.Android
{
    public class AndroidBleManager : IBleManager
    {
        #region BleScanCallback

        protected class BleScanCallback : ScanCallback
        {
            readonly BleDeviceFilter _filter;
            readonly TaskCompletionSource<BluetoothDevice?> _source;

            public BleScanCallback(BleDeviceFilter filter)
            {
                _source = new();
                _filter = filter;
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
            {
                if (_filter.Name != null && result?.Device?.Name == _filter.Name)
                    _source.SetResult(result.Device);

                base.OnScanResult(callbackType, result);
            }

            public override void OnBatchScanResults(IList<ScanResult>? results)
            {
                base.OnBatchScanResults(results);
            }

            public override void OnScanFailed(ScanFailure errorCode)
            {
                _source.SetException(new Exception(errorCode.ToString()));
                base.OnScanFailed(errorCode);
            }


            public Task<BluetoothDevice?> Task => _source.Task;
        }

        #endregion


        private readonly BluetoothManager _bltManager;
        private readonly BluetoothAdapter _adapter;

        public AndroidBleManager()
        {
            _bltManager = (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService)!;
            _adapter = _bltManager.Adapter!;
        }

        public async  Task<IList<BleDeviceInfo>> FindDevicesAsync(BleDeviceFilter filter)
        {
            var result = new List<BleDeviceInfo>();

            void AddDevice(BluetoothDevice device)
            {
                if (filter.Name != null && device.Name != filter.Name)
                    return;

                result.Add(new BleDeviceInfo
                {
                    Address = ulong.Parse(device.Address!.Replace(":", ""), System.Globalization.NumberStyles.HexNumber),
                    Name = device.Name
                });
            }

            foreach (var device in _adapter.BondedDevices!)
                AddDevice(device);


            if (filter.MaxDevices > 0 && result.Count >= filter.MaxDevices)
                return result;

            var scanner = _adapter.BluetoothLeScanner!;

            var scanSettings = new ScanSettings.Builder()
                .SetScanMode(global::Android.Bluetooth.LE.ScanMode.LowLatency)! 
                .Build()!;


            var scanCallback = new BleScanCallback(filter);
            scanner.StartScan(null, scanSettings, scanCallback);

            try
            {
                var device = await scanCallback.Task.WaitAsync(filter.Timeout);

                if (device != null)
                    AddDevice(device);
            }
            catch
            {

            }
            return result;
        }

        public Task<IBleDevice> GetDeviceAsync(BleAddress address)
        {
            var device = _adapter.GetRemoteDevice(BitConverter.GetBytes(address.Value).Take(6).Reverse().ToArray());    
            
            if (device == null)
                throw new InvalidOperationException();  

            return Task.FromResult<IBleDevice>(new AndroidBleDevice(device));
        }
    }
}

#endif
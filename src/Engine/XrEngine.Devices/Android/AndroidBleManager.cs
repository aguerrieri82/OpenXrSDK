#if __ANDROID__

using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using static Android.Bluetooth.BluetoothAdapter;

namespace XrEngine.Devices.Android
{
    public class AndroidBleManager : IBleManager
    {
        #region BleScanCallback

        protected class BleScanCallback : ScanCallback
        {

            TaskCompletionSource<BluetoothDevice?> _source;

            public BleScanCallback()
            {
                _source = new();
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
            {
                if (result?.Device?.Name == "Pedal Controller")
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
            var scanner = _adapter.BluetoothLeScanner!;

            var scanCallback = new BleScanCallback();
            scanner.StartScan(scanCallback);

            try
            {
                var device = await scanCallback.Task.WaitAsync(filter.Timeout);

                if (device != null)
                {
                    return
                    [
                        new BleDeviceInfo
                        {
                            Address = ulong.Parse(device.Address!.Replace(":", ""), System.Globalization.NumberStyles.HexNumber),
                            Name = device.Name
                        }
                    ];
                }
            }
            catch
            {

            }
            return [];
        }

        public Task<IBleDevice> GetDeviceAsync(BleAddress address)
        {
            throw new NotImplementedException();
        }
    }
}

#endif
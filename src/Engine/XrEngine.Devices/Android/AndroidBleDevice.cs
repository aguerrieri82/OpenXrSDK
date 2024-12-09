#if ANDROID21_0_OR_GREATER

using Android.Bluetooth;
using Android.Runtime;
using Java.Util;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#pragma warning disable CA1422
#pragma warning disable CA1416


namespace XrEngine.Devices.Android
{
    public class AndroidBleDevice : IBleDevice
    {

        #region GattCallback

        private class GattCallback : BluetoothGattCallback
        {
            private readonly AndroidBleDevice _host;

            private TaskCompletionSource<GattStatus>? _discServices;
            private TaskCompletionSource<GattStatus>? _connect;
            private TaskCompletionSource<GattStatus>? _write;
            private TaskCompletionSource<byte[]>? _read;
            private BluetoothGattDescriptor? _readDesc;
            private BluetoothGattCharacteristic? _readCts;
            private BluetoothGattCharacteristic? _writeCts;

            public GattCallback(AndroidBleDevice host)
            {
                _host = host;
                _connect = new TaskCompletionSource<GattStatus>();
            }

            public override void OnServicesDiscovered(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status)
            {
                _discServices?.SetResult(status);

                base.OnServicesDiscovered(gatt, status);
            }

            public override void OnConnectionStateChange(BluetoothGatt? gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
            {
                Debug.Assert(gatt != null);

                if (newState == ProfileState.Connected)
                {
                    IsConnected = true;
                }
                else if (newState == ProfileState.Disconnected)
                {
                    gatt.Close();
                    IsConnected = false;
                }

                _connect?.SetResult(status);
            }
            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
            {

                base.OnCharacteristicChanged(gatt, characteristic, value);
            }

            public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
            {
                _host.OnCharacteristicChanged(Guid.Parse(characteristic!.Uuid!.ToString()), characteristic.GetValue()!);

                base.OnCharacteristicChanged(gatt, characteristic);
            }

            public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, [GeneratedEnum] GattStatus status)
            {
                if (_readCts == characteristic && _read != null)
                    _read.SetResult(characteristic!.GetValue()!);
                base.OnCharacteristicRead(gatt, characteristic, status);
            }

            public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, [GeneratedEnum] GattStatus status)
            {
                if (_writeCts == characteristic && _write != null)
                    _write.SetResult(status);
                base.OnCharacteristicWrite(gatt, characteristic, status);
            }



            public override void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, [GeneratedEnum] GattStatus status)
            {
                if (_readDesc == descriptor && _read != null)
                    _read.SetResult(descriptor!.GetValue()!);
                base.OnDescriptorRead(gatt, descriptor, status);
            }

            internal void BeginRead(BluetoothGattDescriptor desc)
            {
                _read = new TaskCompletionSource<byte[]>();
                _readDesc = desc;
            }

            internal void BeginRead(BluetoothGattCharacteristic cts)
            {
                _read = new TaskCompletionSource<byte[]>();
                _readCts = cts;
            }

            internal void BeginWrite(BluetoothGattCharacteristic cts)
            {
                _write = new TaskCompletionSource<GattStatus>();
                _writeCts = cts;
            }

            internal void BeginDiscover()
            {
                _discServices = new TaskCompletionSource<GattStatus>();
            }

            public Task<GattStatus> DiscoverServicesTask => _discServices?.Task ?? throw new InvalidOperationException();

            public Task<GattStatus> ConnectTask => _connect?.Task ?? throw new InvalidOperationException();

            public Task<byte[]> ReadTask => _read?.Task ?? throw new InvalidOperationException();

            public Task<GattStatus> WriteTask => _write?.Task ?? throw new InvalidOperationException();

            public bool IsConnected;
        }

        #endregion

        #region ValueChangedHandler
        struct ValueChangedHandler
        {
            public BleCharacteristicValueChangedDelegate Handler;

            public BleCharacteristicInfo Info;
        }

        #endregion


        private readonly BluetoothDevice _device;
        private BluetoothGatt? _gatt;
        private GattCallback? _gattCb;
        private List<ValueChangedHandler> _changedHandlers = [];

        public AndroidBleDevice(BluetoothDevice device)
        {
            _device = device;
        }

        public async Task ConnectAsync()
        {
            _gattCb = new GattCallback(this);

            _gatt = _device.ConnectGatt(Application.Context, true, _gattCb);
            
            var res = await _gattCb.ConnectTask;

            if (res != GattStatus.Success)
                throw new InvalidOperationException(res.ToString());
        }

        public Task DisconnectAsync()
        {
            if (_gatt != null)
            {
                _gatt.Disconnect();
                _gatt = null;
            }

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<BleServiceInfo>> GetServicesAsync(int timeoutMs)
        {
            if (_gatt == null || _gattCb == null)
                throw new InvalidOperationException("Not connected");

            _gattCb.BeginDiscover();

            _gatt.DiscoverServices();

            while (true)
            {
                var task = _gattCb.DiscoverServicesTask;
                
                if (timeoutMs > 0)
                    task = task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
                
                var res = await task;
                if (res == GattStatus.Success)
                    break;

                await Task.Delay(200);
            }

            return _gatt.Services!.Select(a => new BleServiceInfo { Id = Guid.Parse(a.Uuid!.ToString()) });
        }

        async Task<string?> GetNameAsync(BluetoothGattCharacteristic cts)
        {
            Debug.Assert(_gattCb != null && _gatt != null);

            var desc = cts.GetDescriptor(UUID.FromString("00002901-0000-1000-8000-00805f9b34fb"));

            if (desc == null)
                return null;

            _gattCb.BeginRead(desc);

            if (!_gatt.ReadDescriptor(desc))
                return null;

            var value = await _gattCb.ReadTask;

            if (value == null)
                return null;

            return Encoding.UTF8.GetString(value!);
        }

        public async Task<IEnumerable<BleCharacteristicInfo>> GetCharacteristicsAsync(BleServiceInfo serviceInfo, int timeoutMs)
        {
            if (_gatt == null || _gattCb == null)
                throw new InvalidOperationException("Not connected");

            var service = _gatt.GetService(ToUUID(serviceInfo.Id)) ?? 
                throw new InvalidOperationException("Service not found");

            var result = new List<BleCharacteristicInfo>();

            foreach (var a in service.Characteristics!)
            {
                result.Add(new BleCharacteristicInfo
                {
                    Id = ToGuid(a.Uuid!),
                    Name = await GetNameAsync(a),
                    Service = serviceInfo
                });
            }

            return result;
        }

        public async Task<byte[]> ReadCharacteristicAsync(BleCharacteristicInfo characteristicInfo)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);

            if ((cts.Properties & GattProperty.Read) == 0)
                throw new InvalidOperationException("Characteristic is not readable");

            _gattCb.BeginRead(cts);

            int attempt = 0;

            while (true)
            {
                if (_gatt.ReadCharacteristic(cts))
                    break;

                attempt++;

                if (attempt > 3)
                    throw new InvalidOperationException("");

                await Task.Delay(200);
            }
            return await _gattCb.ReadTask;
        }

        public async Task WriteCharacteristicAsync(BleCharacteristicInfo characteristicInfo, byte[] data)
        {
            Debug.Assert(_gattCb != null);

            var cts = GetCharacteristicInternal(characteristicInfo);

            _gattCb.BeginWrite(cts);

            cts.SetValue(data);

            if (!_gatt.WriteCharacteristic(cts))
                throw new InvalidOperationException("Failed to initiate write");

            await _gattCb.WriteTask;
        }

        public Task WriteCharacteristicConfigurationAsync(BleCharacteristicInfo characteristicInfo, BleCharacteristicConfig value)
        {
            var cts = GetCharacteristicInternal(characteristicInfo);

            if (value == BleCharacteristicConfig.Notify)
                _gatt.SetCharacteristicNotification(cts, true);

            var descriptor = cts.GetDescriptor(
                UUID.FromString("00002902-0000-1000-8000-00805f9b34fb")
            );

            if (descriptor != null)
            {
                descriptor.SetValue(value switch
                {
                    BleCharacteristicConfig.Notify => BluetoothGattDescriptor.EnableNotificationValue!.ToArray(),
                    BleCharacteristicConfig.Indicate => BluetoothGattDescriptor.EnableIndicationValue!.ToArray(),
                    _ => [],
                });

                _gatt.WriteDescriptor(descriptor);
            }

            return Task.CompletedTask;
        }

        public void AddCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler)
        {
            if (!_changedHandlers.Any(a => a.Info == characteristicInfo && a.Handler == handler))
            {
                _changedHandlers.Add(new ValueChangedHandler
                {
                    Handler = handler,
                    Info = characteristicInfo
                });
            }   
        }

        public void RemoveCharacteristicValueChangedHandler(BleCharacteristicInfo characteristicInfo, BleCharacteristicValueChangedDelegate handler)
        {
            _changedHandlers.RemoveAll(a => a.Info == characteristicInfo && a.Handler == handler);
        }

        [MemberNotNull(nameof(_gatt), nameof(_gattCb))]
        BluetoothGattCharacteristic GetCharacteristicInternal(BleCharacteristicInfo characteristicInfo)
        {
            if (_gatt == null || _gattCb == null)
                throw new InvalidOperationException("Not connected");

            return _gatt.GetService(ToUUID(characteristicInfo.Service!.Id))!.
                         GetCharacteristic(ToUUID(characteristicInfo.Id))!;
        }

        protected virtual void OnCharacteristicChanged(Guid ctsId, byte[] value)
        {
            foreach (var handler in _changedHandlers.Where(a => a.Info.Id == ctsId))
                handler.Handler(handler.Info, value); 
        }

        static UUID ToUUID(Guid guid) => UUID.FromString(guid.ToString())!;

        static Guid ToGuid(UUID uuid) => new(uuid.ToString());

        public bool IsConnected => _gatt != null && _gattCb != null && _gattCb.IsConnected;

    }
}

#endif
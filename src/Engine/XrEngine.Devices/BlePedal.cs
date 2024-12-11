
using System.Runtime.InteropServices;

namespace XrEngine.Devices
{
    public class DataEventArgs<T> : EventArgs
    {
        public DataEventArgs(T data)
        {
            Data = data;
        }

        public T Data { get; }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct BlePedalData
    {
        public uint Timestamp;

        public int Value;

        public int DeltaTime;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct BlePedalSettings
    {
        public uint Size;

        public int Key;

        public byte Mode;

        public int SampleRate;

        public int RampUp;

        public int RampHit;

        public int RampDown;
    }

    public class BlePedal : IAsyncDisposable
    {
        readonly IBleManager _manager;

        IBleDevice? _device;
        BleServiceInfo? _mainService;
        BleCharacteristicInfo? _settingsChar;
        BleCharacteristicInfo? _valueChar;
        BleCharacteristicInfo? _batteryChar;

        static readonly Guid MAIN_SERVICE = Guid.Parse("a10bbd49-a988-4fc7-bc7f-58a672d3d653");
        static readonly Guid SETTINGS_UUID = BleUUID.FromInt(0x1);
        static readonly Guid BATTERY_UUID = BleUUID.FromInt(0x2);
        static readonly Guid VALUE_UUID = BleUUID.FromInt(0x3);

        public BlePedal(IBleManager manager)
        {
            _manager = manager;
        }

        public async Task UpdateSettingsAsync(BlePedalSettings value)
        {
            if (_device == null || _settingsChar == null)
                throw new InvalidOperationException();

            value.Size = (uint)Marshal.SizeOf<BlePedalSettings>();
            value.Key = 1397052500;

            var buf = StructToBytes(value);

            await _device.WriteCharacteristicAsync(_settingsChar, buf);
        }

        public async Task DisconnectAsync()
        {
            if (_device != null)
            {
                await _device.DisconnectAsync();
                _device = null;
            }
        }

        public async Task ConnectAsync(BleAddress address, int timeoutMs = 0)
        {
            _device = await _manager.GetDeviceAsync(address);

            await _device.ConnectAsync();

            var services = await _device.GetServicesAsync(timeoutMs);

            _mainService = services.Single(a => a.Id == MAIN_SERVICE);

            var chars = await _device.GetCharacteristicsAsync(_mainService, timeoutMs);

            _valueChar = chars.Single(a => a.Id == VALUE_UUID);
            _settingsChar = chars.Single(a => a.Id == SETTINGS_UUID);
            _batteryChar = chars.Single(a => a.Id == BATTERY_UUID);

            await _device.WriteCharacteristicConfigurationAsync(_valueChar!, BleCharacteristicConfig.Notify);

            _device.AddCharacteristicValueChangedHandler(_valueChar, OnChanged);
        }

        public async Task<float> GetBatteryAsync()
        {
            var data = await _device!.ReadCharacteristicAsync(_batteryChar!);

            var raw = BytesToStruct<uint>(data);

            float voltageADC = (raw / 4095f) * 3.3f;
            float batteryVoltage = voltageADC * (100000f + 100000f) / 100000f;

            return batteryVoltage;
        }

        public async Task<BlePedalSettings> ReadSettingsAsync()
        {
            var data = await _device!.ReadCharacteristicAsync(_settingsChar!);
            return BytesToStruct<BlePedalSettings>(data);
        }

        static unsafe T BytesToStruct<T>(byte[] data) where T : unmanaged
        {
            fixed (byte* pBuf = data)
                return *(T*)pBuf;
        }

        static unsafe byte[] StructToBytes<T>(T value) where T : unmanaged
        {
            return new Span<byte>(&value, sizeof(T)).ToArray();
        }

        private unsafe void OnChanged(BleCharacteristicInfo characteristicInfo, byte[] value)
        {
            fixed (byte* pData = value)
            {
                var pedalData = *(BlePedalData*)pData;
                Data?.Invoke(this, new DataEventArgs<BlePedalData>(pedalData));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
            GC.SuppressFinalize(this);
        }

        public bool IsConnected => _device?.IsConnected ?? false;


        public event EventHandler<DataEventArgs<BlePedalData>>? Data;
    }
}

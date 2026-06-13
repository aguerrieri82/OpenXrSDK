#if __ANDROID__

using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using XrEngine.Devices;
using ContextA = global::Android.Content.Context;
using ActivityA = global::Android.App.Activity;


namespace XrEngine.Android.Devices
{
    public sealed class AndroidUsbCameraManager : Java.Lang.Object, ICameraManager, IDisposable
    {
        private readonly ActivityA _activity;
        private readonly UsbManager _usbManager;
        private readonly string _permissionAction;

        private UsbPermissionReceiver? _receiver;
        private bool _receiverRegistered;

        private readonly Dictionary<string, TaskCompletionSource<bool>> _permissionRequests = new();

        public AndroidUsbCameraManager(ActivityA activity)
        {
            _activity = activity;
            _usbManager = (UsbManager)activity.GetSystemService(ContextA.UsbService)!;
            _permissionAction = $"{activity.PackageName}.USB_PERMISSION";

            Context.Implement(this);
        }

        public void Start()
        {
            RegisterReceiver();
        }

        public void Stop()
        {
            UnregisterReceiver();
        }

        public IList<CameraDeviceInfo> GetCameras()
        {
            var result = new List<CameraDeviceInfo>();

            foreach (var item in _usbManager.DeviceList!)
            {
                var device = item.Value;

                if (!IsVideoDevice(device))
                    continue;

                result.Add(new CameraDeviceInfo
                {
                    Id = device.DeviceName,
                    Name = GetDeviceName(device),
                    Position = null,
                    Facing = null,
                    Source = null
                });
            }

            return result;
        }

        public async Task<ICameraDevice> OpenCameraAsync(string id)
        {
            var device = FindDevice(id);

            if (device == null)
                throw new InvalidOperationException($"USB camera not found: {id}");

            RegisterReceiver();

            if (!_usbManager.HasPermission(device))
            {
                bool granted = await RequestPermissionAsync(device).ConfigureAwait(false);

                if (!granted)
                    throw new UnauthorizedAccessException($"USB permission denied: {id}");
            }

            var connection = _usbManager.OpenDevice(device);

            if (connection == null)
                throw new InvalidOperationException($"UsbManager.OpenDevice failed: {id}");

            try
            {
                int fd = connection.FileDescriptor;

                var camera = new UsbCamera(
                    fd,
                    device.VendorId,
                    device.ProductId);

                await camera.OpenAsync();

                /*
                   Native OpenDeviceFd duplicates fd.
                   After OpenAsync succeeds, the Java UsbDeviceConnection can be closed.
                */
                connection.Close();

                return camera;
            }
            catch
            {
                connection.Close();
                throw;
            }
        }

        public new void Dispose()
        {
            Stop();

            foreach (var request in _permissionRequests.Values)
                request.TrySetCanceled();

            _permissionRequests.Clear();

            base.Dispose();
        }

        private UsbDevice? FindDevice(string id)
        {
            foreach (var item in _usbManager.DeviceList!)
            {
                var device = item.Value;

                if (device.DeviceName == id)
                    return device;
            }

            return null;
        }

        private Task<bool> RequestPermissionAsync(UsbDevice device)
        {
            string key = device.DeviceName;

            if (_permissionRequests.TryGetValue(key, out var existing))
                return existing.Task;

            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _permissionRequests[key] = tcs;

            var intent = new Intent(_permissionAction);
            intent.SetPackage(_activity.PackageName);

            var flags = PendingIntentFlags.UpdateCurrent;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                flags |= PendingIntentFlags.Mutable;

            var pendingIntent = PendingIntent.GetBroadcast(
                _activity,
                device.DeviceId,
                intent,
                flags);

            _usbManager.RequestPermission(device, pendingIntent);

            return tcs.Task;
        }

        private void CompletePermission(UsbDevice? device, bool granted)
        {
            if (device == null)
                return;

            string key = device.DeviceName;

            if (_permissionRequests.Remove(key, out var tcs))
                tcs.TrySetResult(granted);
        }

        private void RegisterReceiver()
        {
            if (_receiverRegistered)
                return;

            _receiver = new UsbPermissionReceiver(this);

            var filter = new IntentFilter(_permissionAction);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                _activity.RegisterReceiver(_receiver, filter, ReceiverFlags.NotExported);
            else
                _activity.RegisterReceiver(_receiver, filter);

            _receiverRegistered = true;
        }

        private void UnregisterReceiver()
        {
            if (!_receiverRegistered || _receiver == null)
                return;

            try
            {
                _activity.UnregisterReceiver(_receiver);
            }
            catch (Java.Lang.IllegalArgumentException)
            {
            }

            _receiverRegistered = false;
            _receiver = null;
        }

        private static bool IsVideoDevice(UsbDevice device)
        {
            if (device.DeviceClass == UsbClass.Video)
                return true;

            for (int i = 0; i < device.InterfaceCount; i++)
            {
                var intf = device.GetInterface(i);

                if (intf.InterfaceClass == UsbClass.Video)
                    return true;
            }

            return false;
        }

        private static string GetDeviceName(UsbDevice device)
        {
            string? product = null;
            string? manufacturer = null;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                product = device.ProductName;
                manufacturer = device.ManufacturerName;
            }

            if (!string.IsNullOrWhiteSpace(product) &&
                !string.IsNullOrWhiteSpace(manufacturer))
            {
                return $"{manufacturer} {product}";
            }

            if (!string.IsNullOrWhiteSpace(product))
                return product!;

            return $"USB camera {device.VendorId:X4}:{device.ProductId:X4}";
        }

        private sealed class UsbPermissionReceiver : BroadcastReceiver
        {
            private readonly AndroidUsbCameraManager _owner;

            public UsbPermissionReceiver(AndroidUsbCameraManager owner)
            {
                _owner = owner;
            }

            public override void OnReceive(ContextA? context, Intent? intent)
            {
                if (intent?.Action != _owner._permissionAction)
                    return;

                var device = (UsbDevice?)intent.GetParcelableExtra(UsbManager.ExtraDevice);
                bool granted = intent.GetBooleanExtra(UsbManager.ExtraPermissionGranted, false);

                _owner.CompletePermission(device, granted);
            }
        }
    }
}

#endif
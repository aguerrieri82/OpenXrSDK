#if __ANDROID__

using Android;
using Android.Content.PM;

[assembly: UsesPermission("horizonos.permission.HEADSET_CAMERA")]
[assembly: UsesPermission("horizonos.permission.USB_CAMERA")]
[assembly: UsesPermission("horizonos.permission.CAMERA")]

[assembly: UsesFeature(PackageManager.FeatureCameraAny, Required = false)]
[assembly: UsesFeature(PackageManager.FeatureCameraExternal, Required = false)]
[assembly: UsesFeature(PackageManager.FeatureCamera, Required = false)]
[assembly: UsesFeature(PackageManager.FeatureUsbHost, Required = false)]


[assembly: UsesPermission(Manifest.Permission.Bluetooth)]
[assembly: UsesPermission(Manifest.Permission.BluetoothAdmin)]
[assembly: UsesPermission(Manifest.Permission.BluetoothScan)]
[assembly: UsesPermission(Manifest.Permission.BluetoothConnect)]

#endif
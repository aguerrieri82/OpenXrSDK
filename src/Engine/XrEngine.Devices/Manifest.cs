#if __ANDROID__

[assembly: UsesPermission("horizonos.permission.HEADSET_CAMERA")]
[assembly: UsesPermission("horizonos.permission.USB_CAMERA")]
[assembly: UsesPermission("horizonos.permission.CAMERA")]

[assembly: UsesFeature("android.hardware.camera2.any", Required = false)]
[assembly: UsesFeature("android.hardware.camera.external", Required = false)]
[assembly: UsesFeature("android.hardware.camera", Required = false)]
[assembly: UsesFeature("android.hardware.usb.host", Required = false)]


[assembly: UsesPermission("android.permission.BLUETOOTH")]
[assembly: UsesPermission("android.permission.BLUETOOTH_ADMIN")]
[assembly: UsesPermission("android.permission.BLUETOOTH_SCAN")]
[assembly: UsesPermission("android.permission.BLUETOOTH_CONNECT")]

#endif
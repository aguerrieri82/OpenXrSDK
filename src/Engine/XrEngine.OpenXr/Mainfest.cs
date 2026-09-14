#if __ANDROID__

using Android.App;

[assembly: UsesPermission("android.permission.MODIFY_AUDIO_SETTINGS")]
[assembly: UsesPermission("android.permission.ACCESS_NETWORK_STATE")]
[assembly: UsesPermission("android.permission.MANAGE_EXTERNAL_STORAGE")]

#endif
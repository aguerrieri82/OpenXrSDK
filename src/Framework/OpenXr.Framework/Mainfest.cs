#if __ANDROID__

using Android.App;

[assembly: UsesPermission("org.khronos.openxr.permission.OPENXR")]
[assembly: UsesFeature("android.hardware.vr.headtracking", Required = true)]

#endif
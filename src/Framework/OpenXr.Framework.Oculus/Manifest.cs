#if __ANDROID__

using Android.App;
using OpenXr.Framework.Oculus;

[assembly: UsesPermission("android.permission.RECORD_AUDIO")]

[assembly: UsesPermission(OculusPermissions.RenderModel)]
[assembly: UsesPermission(OculusPermissions.BodyTracking)]
[assembly: UsesPermission(OculusPermissions.AccessTrackingEnv)]
[assembly: UsesPermission(OculusPermissions.UseScene)]
[assembly: UsesPermission(OculusPermissions.UseAnchorApi)]
[assembly: UsesPermission(OculusPermissions.HandTracking)]
[assembly: UsesPermission(OculusPermissions.FaceTracking)]
[assembly: UsesPermission(OculusPermissions.EyeTracking)]

[assembly: UsesFeature("oculus.software.vr.app.hybrid")]
[assembly: UsesFeature("oculus.software.overlay_keyboard")]
[assembly: UsesFeature("oculus.software.handtracking")]
[assembly: UsesFeature("oculus.software.face_tracking", Required = false)]
[assembly: UsesFeature("oculus.software.eye_tracking", Required = false)]
[assembly: UsesFeature("com.oculus.software.body_tracking")]
[assembly: UsesFeature("com.oculus.feature.VIRTUAL_KEYBOARD", Required = false)]
[assembly: UsesFeature("com.oculus.feature.RENDER_MODEL", Required = false)]
[assembly: UsesFeature("com.oculus.feature.PASSTHROUGH")]
[assembly: UsesFeature("com.oculus.feature.CONTEXTUAL_BOUNDARYLESS_APP", Required = false)]
[assembly: UsesFeature("com.oculus.experimental.enabled", Required = false)]

#endif
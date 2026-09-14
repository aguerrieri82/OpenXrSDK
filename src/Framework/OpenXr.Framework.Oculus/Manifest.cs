#if __ANDROID__

using Android;
using Android.App;
using OpenXr.Framework.Oculus;

[assembly: UsesPermission(Manifest.Permission.RecordAudio)]

[assembly: UsesPermission(OculusPermissions.RenderModel)]
[assembly: UsesPermission(OculusPermissions.BodyTracking)]
[assembly: UsesPermission(OculusPermissions.AccessTrackingEnv)]
[assembly: UsesPermission(OculusPermissions.UseScene)]
[assembly: UsesPermission(OculusPermissions.UseAnchorApi)]
[assembly: UsesPermission(OculusPermissions.HandTracking)]
[assembly: UsesPermission(OculusPermissions.FaceTracking)]
[assembly: UsesPermission(OculusPermissions.EyeTracking)]

[assembly: UsesFeature(OculusFeatures.HybridApp)]
[assembly: UsesFeature(OculusFeatures.OverlayKeyboard)]
[assembly: UsesFeature(OculusFeatures.HandTracking)]
[assembly: UsesFeature(OculusFeatures.FaceTracking)]
[assembly: UsesFeature(OculusFeatures.EyeTracking)]
[assembly: UsesFeature(OculusFeatures.BodyTracking)]
[assembly: UsesFeature(OculusFeatures.VirtualKeyboard)]
[assembly: UsesFeature(OculusFeatures.RenderModel)]
[assembly: UsesFeature(OculusFeatures.Passthrough)]
[assembly: UsesFeature(OculusFeatures.ContextualBoundarylessApp)]
[assembly: UsesFeature(OculusFeatures.ExperimentalEnabled)]

#endif
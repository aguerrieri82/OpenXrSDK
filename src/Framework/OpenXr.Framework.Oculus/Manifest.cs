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
[assembly: UsesFeature(OculusFeatures.OverlayKeyboard, Required = false)]
[assembly: UsesFeature(OculusFeatures.HandTracking, Required = false)]
[assembly: UsesFeature(OculusFeatures.FaceTracking, Required = false)]
[assembly: UsesFeature(OculusFeatures.EyeTracking, Required = false)]
[assembly: UsesFeature(OculusFeatures.BodyTracking, Required = false)]
[assembly: UsesFeature(OculusFeatures.VirtualKeyboard, Required = false)]
[assembly: UsesFeature(OculusFeatures.RenderModel, Required = false)]
[assembly: UsesFeature(OculusFeatures.Passthrough)]
[assembly: UsesFeature(OculusFeatures.ContextualBoundarylessApp)]
[assembly: UsesFeature(OculusFeatures.ExperimentalEnabled)]

#endif
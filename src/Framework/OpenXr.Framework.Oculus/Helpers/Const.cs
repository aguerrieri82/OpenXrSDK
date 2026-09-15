namespace OpenXr.Framework.Oculus
{
    public static class OculusIntentCategory
    {
        public const string TwoD = "com.oculus.intent.category.2D";
        public const string Vr = "com.oculus.intent.category.VR";
    }

    public static class OculusPermissions
    {
        public const string RenderModel = "com.oculus.permission.RENDER_MODEL";
        public const string BodyTracking = "com.oculus.permission.BODY_TRACKING";
        public const string AccessTrackingEnv = "com.oculus.permission.ACCESS_TRACKING_ENV";
        public const string UseScene = "com.oculus.permission.USE_SCENE";
        public const string UseAnchorApi = "com.oculus.permission.USE_ANCHOR_API";
        public const string HandTracking = "com.oculus.permission.HAND_TRACKING";
        public const string FaceTracking = "com.oculus.permission.FACE_TRACKING";
        public const string EyeTracking = "com.oculus.permission.EYE_TRACKING";

    }

    public static class HorizonPermissions
    {
        public const string HeadsetCamera = "horizonos.permission.HEADSET_CAMERA";
        public const string UsbCamera = "horizonos.permission.USB_CAMERA";
        public const string Camera = "horizonos.permission.CAMERA";
    }

    public static class OculusFeatures
    {
        public const string HybridApp = "oculus.software.vr.app.hybrid";
        public const string OverlayKeyboard = "oculus.software.overlay_keyboard";
        public const string HandTracking = "oculus.software.handtracking";
        public const string FaceTracking = "oculus.software.face_tracking";
        public const string EyeTracking = "oculus.software.eye_tracking";

        public const string BodyTracking = "com.oculus.software.body_tracking";
        public const string VirtualKeyboard = "com.oculus.feature.VIRTUAL_KEYBOARD";
        public const string RenderModel = "com.oculus.feature.RENDER_MODEL";
        public const string Passthrough = "com.oculus.feature.PASSTHROUGH";
        public const string ContextualBoundarylessApp = "com.oculus.feature.CONTEXTUAL_BOUNDARYLESS_APP";
        public const string ExperimentalEnabled = "com.oculus.experimental.enabled";
    }
}

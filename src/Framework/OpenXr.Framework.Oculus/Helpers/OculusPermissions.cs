using System;
using System.Collections.Generic;
using System.Text;

namespace OpenXr.Framework.Oculus
{
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
}

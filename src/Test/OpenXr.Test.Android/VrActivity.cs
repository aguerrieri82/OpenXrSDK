using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Silk.NET.OpenXR;
using static Android.Telephony.CarrierConfigManager;


namespace OpenXr.Test.Android
{
    [IntentFilter(["com.oculus.intent.category.VR"])]
    [Activity(
        Label = "@string/app_name",
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        Exported = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Landscape)]
    [MetaData("com.samsung.android.vr.application.mode", Value = "vr_only")]
    public class VrActivity : Activity
    {
        const string TAG = nameof(VrActivity);

        const int REQUEST_CODE_PERMISSION_USE_SCENE = 1;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestScenePermissionIfNeeded("com.oculus.permission.USE_SCENE");

            GlobalServices.App!.Plugin<OpenVrPlugin>().RegisterVrActivity(this);
        }

        protected override void OnDestroy()
        {
            GlobalServices.App!.Plugin<OpenVrPlugin>().RegisterVrActivity(null);
            base.OnDestroy();
        }

        private void RequestScenePermissionIfNeeded(string perm)
        {
            Log.Debug(TAG, "requestScenePermissionIfNeeded");
            if (CheckSelfPermission(perm) != Permission.Granted)
            {
                Log.Debug(TAG, "Permission has not been granted, request " + perm);
                RequestPermissions([perm], REQUEST_CODE_PERMISSION_USE_SCENE);
            }
        }
    }
}
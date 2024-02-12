using Android.Content;
using Android.Content.PM;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using OpenXr.Samples;
using OpenXr.WebLink.Android;


namespace OpenXr.Test.Android
{

    [IntentFilter([
        "com.oculus.intent.category.VR",
        "android.intent.action.MAIN",
        "android.intent.category.LAUNCHER"])]
    [Activity(
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    LaunchMode = LaunchMode.SingleTask,
    Exported = true,
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
    ScreenOrientation = ScreenOrientation.Landscape)]
    [MetaData("com.samsung.android.vr.application.mode", Value = "vr_only")]
    public class GameActivity : XrActivity
    {


        protected override SampleXrApp CreateApp()
        {
            var options = new OculusXrPluginOptions
            {
                EnableMultiView = true,
                SampleCount = 4
            };

            var logger = new AndroidLogger("XrApp");

            var result = new SampleXrApp(logger,
                 new AndroidXrOpenGLESGraphicDriver(),
                 new OculusXrPlugin(options),
                 new AndroidXrPlugin(this));

            result.Layers.Add<XrPassthroughLayer>();

            result.BindEngineApp(
                Common.CreateScene(new AndroidAssetManager(this)),
                options.SampleCount,
                options.EnableMultiView);

            StartService(new Intent(this, typeof(WebLinkService)));

            return result;
        }
    }
}
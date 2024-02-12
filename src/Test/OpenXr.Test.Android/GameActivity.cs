using Android.Content.PM;
using Android.OS;
using OpenXr.Engine;
using OpenXr.Engine.OpenGL.Oculus;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using OpenXr.Framework.Oculus;
using OpenXr.Samples;


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

            var result = new SampleXrApp(
                 new AndroidXrOpenGLESGraphicDriver(OpenGLESContext.Create()),
                 new OculusXrPlugin(options),
                 new AndroidXrPlugin(this, (uint)Process.MyTid()));

            result.Layers.Add<XrPassthroughLayer>();

            result.BindEngineApp(
                Common.CreateScene(new AndroidAssetManager(this)), 
                options.SampleCount, 
                options.EnableMultiView);

            return result;
        }
    }
}
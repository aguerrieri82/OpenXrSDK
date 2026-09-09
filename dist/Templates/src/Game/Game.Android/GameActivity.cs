using Android.Content;
using Android.Content.PM;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Android;

namespace Game.Android
{
    /*
     * EmbedAssembliesIntoApk = True cause webview to freeze app when load url
     * Remove debug info for real release (DebugSymbols, DebugType, AndroidManagedSymbols)
     */

    [IntentFilter(["android.intent.action.MAIN"],
        Categories = [
        "com.oculus.intent.category.VR",
        "org.khronos.openxr.intent.category.IMMERSIVE_HMD",
        "android.intent.category.LAUNCHER"]
    )]
    [Activity(
        Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        Exported = true,
        MainLauncher = true,
        HardwareAccelerated = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Landscape
    )]

    public class GameActivity : XrEngineActivity
    {
        public GameActivity()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, arg) =>
            {
                if (arg.ExceptionObject is Exception ex)
                    Log.Error(sender, ex);
            };

            TaskScheduler.UnobservedTaskException += (sender, ex) =>
            {
                Log.Error(sender!, ex.Exception);
            };
        }

        protected override void BuildApp(XrEngineAppBuilder builder)
        {
            builder.SetGlOptions(opt =>
            {
                opt.Compression.Use = true;
                opt.SamplerPrecision = ShaderPrecision.Medium;
                opt.FloatPrecision = ShaderPrecision.High;
            })
            .UseOpenGL()
            .SetXrOptions(opt =>
            {
                opt.UseSimmetricFov = false;
            })
            .UseOculus(opt =>
            {
            })
            .UseMultiView()
            .SetRenderQuality(1, 2)
            .CreateGame();
        }
    }
}
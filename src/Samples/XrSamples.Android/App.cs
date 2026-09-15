using Android.Runtime;
using System.Diagnostics;
using XrEngine.Media;
using XrEngine.Media.Android;

namespace XrSamples
{

    [Application(
        Debuggable = true,
        HardwareAccelerated = true,
        UsesCleartextTraffic = true,
        AllowBackup = false,
        Icon = "@mipmap/appicon",
        Label = "@string/app_name",
        SupportsRtl = true)]
    [MetaData("com.oculus.intent.category.VR", Value = "dual")]
    [MetaData("com.oculus.supportedDevices", Value = "all")]
    [MetaData("com.oculus.ossplash.background", Value = "passthrough-contextual")]
    public class App : Application
    {
        public App(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            XrEngine.Context.Implement<SampleManager>();
            XrEngine.Context.Implement<IVideoReader>(() => new AndroidVideoReader());
            XrEngine.Context.Implement<IVideoCodec>(() => new AndroidVideoCodec());

            var envTest = Environment.GetEnvironmentVariable("MONO_ENV_OPTIONS");
            Debug.WriteLine(envTest);

            var manager = XrEngine.Context.Require<SampleManager>();
            manager.AddType(typeof(Dnd.Builder));
            manager.AddType(typeof(Graffiti.Builder));
        }
    }

}

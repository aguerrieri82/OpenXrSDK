using Android.Runtime;
using System.Diagnostics;
using XrEngine;
using XrEngine.Media;
using XrEngine.Media.Android;

namespace XrSamples
{

    [Application(Debuggable = true, HardwareAccelerated = true)]
    public class App : Application
    {
        public App(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {

        }

        public override void OnCreate()
        {
            base.OnCreate();

            bool isAttached = EngineNativeLib.RdcIsAttached();
            global::Android.Util.Log.Warn("App", "RENDER DOC: {0}", isAttached);

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

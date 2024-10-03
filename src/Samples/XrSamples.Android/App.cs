using Android.Runtime;
using System.Diagnostics;
using XrEngine.Media.Android;
using XrEngine.Video;

namespace XrSamples
{
    [Application]
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

        }
    }
}

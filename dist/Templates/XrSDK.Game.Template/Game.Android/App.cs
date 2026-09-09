using Android.Runtime;

namespace $ext_safeprojectname$.Android
{
    [Application(Debuggable = true, HardwareAccelerated = true)]
    [MetaData("com.oculus.intent.category.VR", Value = "dual")]
    [MetaData("com.oculus.supportedDevices", Value = "all")]
    public class App : Application
    {
        public App(nint handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}

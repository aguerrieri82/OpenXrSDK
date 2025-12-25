using XrEngine;

#pragma warning disable CA1416 

[assembly: Module(typeof(XrEngine.Devices.Module))]

namespace XrEngine.Devices
{
    public class Module : IModule
    {
        public void Load()
        {
#if ANDROID

            Context.Implement<ICameraManager>(() => new Android.AndroidCamera2Manager());
            Context.Implement<IBleManager>(() => new Android.AndroidBleManager());
            Context.Implement<IMidiManager>(() => new Android.AndroidMidiManager());
#else
            Context.Implement<IBleManager>(() => new Windows.WinBleManager());
            Context.Implement<IMidiManager>(() => new Windows.WinMidiManager());
#endif
        }

        public void Shutdown()
        {

        }
    }
}


using XrEngine;

[assembly: Module(typeof(XrEngine.Media.Windows.Module))]

namespace XrEngine.Media.Windows
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<IAudioDecoder>(() => new MfAudioDecoder());
            Context.Implement<IAudioEncoder>(() => new MfAudioEncoder());
           
        }

        public void Shutdown()
        {

        }
    }
}


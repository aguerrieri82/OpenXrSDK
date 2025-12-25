using XrEngine;

[assembly: Module(typeof(XrEngine.Media.Android.Module))]

namespace XrEngine.Media.Android
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<IAudioDecoder>(() => new AndroidAudioDecoder());
            Context.Implement<IMediaPlayer>(() => new AndroidMediaPlayer());
            Context.Implement<IVideoReader>(() => new AndroidVideoReader());
            Context.Implement<IVideoCodec>(() => new AndroidVideoCodec());
        }

        public void Shutdown()
        {

        }
    }
}


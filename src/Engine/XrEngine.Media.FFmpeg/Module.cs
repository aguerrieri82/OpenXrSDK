using XrEngine;

[assembly: Module(typeof(XrEngine.Media.FFmpeg.Module))]

namespace XrEngine.Media.FFmpeg
{
    public class Module : IModule
    {
        public void Load()
        {
            Context.Implement<IVideoReader>(() => new FFmpegVideoReader());
            Context.Implement<IVideoCodec>(() => new FFmpegCodec());
        }

        public void Shutdown()
        {

        }
    }
}


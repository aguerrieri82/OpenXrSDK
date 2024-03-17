using OpenAl.Framework;

namespace XrEngine.Audio
{
    public class AudioSystem : Behavior<Scene3D>
    {
        protected readonly AlDevice _device;

        public AudioSystem()
        {
            _device = new AlDevice();
        }

        public AlDevice Device => _device;
    }
}

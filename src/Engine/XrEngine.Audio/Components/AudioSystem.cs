using OpenAl.Framework;

namespace XrEngine.Audio
{
    public class AudioSystem : Behavior<Scene3D>
    {
        protected readonly AlDevice _device;

        public AudioSystem()
        {
            var devices = AlDevice.ListDevices(false);
            _device = new AlDevice();
        }

        public AlDevice Device => _device;
    }
}

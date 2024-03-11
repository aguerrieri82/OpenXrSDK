using OpenAl.Framework;
using Silk.NET.OpenAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public class AudioSystem : Behavior<Scene>
    {
        protected readonly AlDevice _device;

        public AudioSystem()
        {
            _device = new AlDevice();
        }

        public AlDevice Device => _device;
    }
}

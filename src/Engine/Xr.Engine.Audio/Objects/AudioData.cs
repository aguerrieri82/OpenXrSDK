using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Audio
{
    public class AudioData
    {
        public AudioFormat? Format { get; set; }

        public byte[]? Buffer { get; set; }  
    }
}

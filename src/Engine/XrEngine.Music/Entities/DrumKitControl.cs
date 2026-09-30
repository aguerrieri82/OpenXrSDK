using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Music
{
    public enum DrumControlType
    {
        PedalPosition
    }
    
    public class DrumKitControl
    {
        public DrumControlType Type { get; set; }

        public int MidiCode { get; set; }   
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.OpenXr;

namespace XrSamples
{
    public class GameSettings
    {
        public string? SampleName { get; set; } 

        public string? Hdri { get; set; }

        public int Msaa { get; set; }   

        public GraphicDriver Driver  { get; set; }
}
}

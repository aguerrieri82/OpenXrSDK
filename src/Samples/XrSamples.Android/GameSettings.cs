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
        public string? SampleName { get; set; } = "Bed";

        public string? Hdri { get; set; }

        public int Msaa { get; set; } = 1;

        public GraphicDriver Driver  { get; set; } = GraphicDriver.OpenGL;
    }
}

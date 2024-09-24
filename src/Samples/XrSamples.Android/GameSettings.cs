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

        public int Msaa { get; set; } = 1;

        public GraphicDriver Driver { get; set; } = GraphicDriver.OpenGL;

        public bool IsMultiView { get; set; } = true;

        public bool EnableOutline { get; set; } = true;

        public static GameSettings Bed()
        {
            return new GameSettings
            {
                SampleName = "Bed",
                Msaa = 1,
                Driver = GraphicDriver.OpenGL,
                IsMultiView = true
            };
        }

        public static GameSettings Default()
        {
            return new GameSettings();
        }
    }
}

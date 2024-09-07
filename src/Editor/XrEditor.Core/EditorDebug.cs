using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.OpenXr;
using XrSamples;

namespace XrEditor
{
    public static class EditorDebug
    {
        public static GraphicDriver Driver = GraphicDriver.OpenGL;

        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
              //.UseMultiView()
              //.UseStereo()
              .SetRenderQuality(1, 4) ///samples > 1 cause Filament to fuck up
              .CreateChromeBrowser()
              .Build();
    }
}

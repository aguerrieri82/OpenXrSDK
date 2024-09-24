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
        public static readonly GraphicDriver Driver = GraphicDriver.OpenGL;

        public static readonly bool AutoStartApp = true;

        public static XrEngineApp CreateApp() => new XrEngineAppBuilder()
              //.UseMultiView()
              //.UseStereo()
              .SetRenderQuality(1, Driver == GraphicDriver.FilamentVulkan ? 1u : 1u) ///samples > 1 cause Filament to fuck up
              .CreateRoomManager()
              .Build();
    }
}

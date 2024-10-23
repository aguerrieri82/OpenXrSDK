﻿using XrEngine.OpenGL;
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
              .SetGlOptions(new GlRenderOptions()
              {
                  UsePlanarReflection = true,
              })
              .SetRenderQuality(1, Driver == GraphicDriver.FilamentVulkan ? 1u : 2u) ///samples > 1 cause Filament to fuck up
              .CreateCucina()
              .Build();
    }
}

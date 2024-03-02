using OpenXr.Framework;

namespace Xr.Engine.OpenXr
{
    public enum GraphicDriver
    {
        OpenGL,
        FilamentOpenGL,
        FilamentVulkan
    }


    public class XrEngineAppOptions
    {
        public GraphicDriver Driver { get; set; }

        public XrRenderMode RenderMode { get; set; }

        public float ResolutionScale { get; set; }

        public int MSAA { get; set; }

    }

    public class XrEngineApp
    {
    }
}

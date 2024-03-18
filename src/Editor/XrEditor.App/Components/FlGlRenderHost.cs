using XrEngine;
using XrEngine.Filament;

namespace XrEditor
{
    public class FlGlRenderHost : GlRenderHost
    {
        private FilamentRender? _render;

        public FlGlRenderHost()
            : base(false)
        {
        }

        public unsafe override IRenderEngine CreateRenderEngine()
        {
            _render = new FilamentRender(new FilamentOptions
            {
                WindowHandle = HWnd,
                Context = _glCtx,
                Driver = FilamentLib.FlBackend.OpenGL,
                EnableStereo = false,
                OneViewPerTarget = true,
                MaterialCachePath = "d:\\Materials"
            });

            var ctx = _render.GetContext();

            _hdc = ctx.WinGl.HDc;
            _glCtx = ctx.WinGl.GlCTx;

            return _render;
        }


        public override bool TakeContext()
        {
            try
            {
                return base.TakeContext();
            }
            catch
            {
                return false;
            }
        }

        public override void EnableVSync(bool enable)
        {

        }


        public override void SwapBuffers()
        {
        }

    }
}

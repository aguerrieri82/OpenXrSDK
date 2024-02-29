using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xr.Engine;
using Xr.Engine.Filament;

namespace Xr.Editor.Components
{
    public class FlGlRenderHost : GlRenderHost
    {

        public FlGlRenderHost()
            : base(false)
        {
        }

        public unsafe override IRenderEngine CreateRenderEngine()
        {
            var render = new FilamentRender(new FilamentOptions
            {
                WindowHandle = HWnd,
                Context = _glCtx,
                Driver = FilamentLib.FlBackend.OpenGL,
                EnableStereo = false,
                MaterialCachePath = "d:\\Materials"
            });

            var ctx = render.GetContext();

            _hdc = ctx.WinGl.HDc;
            _glCtx = ctx.WinGl.GlCTx;

            return render;
        }

        protected override void CreateContext(HandleRef hwndParent)
        {
            base.CreateContext(hwndParent);
            ReleaseContext();
        }


        public override void SwapBuffers()
        {
        }
    }
}

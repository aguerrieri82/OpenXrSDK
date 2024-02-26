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
    public class FlRenderHost : GlRenderHost
    {
        const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
        const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;

        public FlRenderHost()
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
                MaterialCachePath = "d:\\Materials"
            });

            var ctx = render.GetContext();

            _hdc = ctx.HDc;
            _glCtx = ctx.GlCTx;

            return render;
        }

        public override void EnableVSync(bool enable)
        {
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

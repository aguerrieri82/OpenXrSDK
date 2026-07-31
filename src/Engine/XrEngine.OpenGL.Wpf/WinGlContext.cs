
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif
using Silk.NET.WGL;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace XrEngine.OpenGL.Wpf
{
    public class WinGlContext : IGlContext
    {
        [ThreadStatic]
        static IGlContext? _current;

        private GL? _gl;
        private nint _hdc;
        private WGL? _wgl;
        private nint _ctx;
        private Thread? _owner;

        WinGlContext()
        {
        }

        public static WinGlContext Attach(nint hdc, nint ctx, GL gl, WGL wgl)
        {
            var result = new WinGlContext
            {
                _gl = gl,
                _hdc = hdc,
                _wgl = wgl,
                _ctx = ctx
            };

            return result;
        }

        public void Dispose()
        {
            _wgl!.DeleteContext(_ctx);
            _ctx = 0;
            _owner = null;
        }

        public void Release()
        {
            if (!_wgl!.MakeCurrent(_hdc, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            _owner = null;

            if (_current == this)
                _current = null;
        }

        public void Take()
        {
            if (!_wgl!.MakeCurrent(_hdc, _ctx))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            _owner = Thread.CurrentThread;

            _current = this;

        }

        public void SwapBuffers()
        {
            _wgl!.SwapBuffers(_hdc);
        }

        public GL Gl => _gl!;

        public Thread? OwnerThread => _owner;

        public static IGlContext? Current => _current;
    }
}

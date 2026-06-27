using OpenXr.Framework;
using OpenXr.Framework.OpenGL;
using Silk.NET.Core.Contexts;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using XrEngine;
using XrEngine.OpenGL;
using Silk.NET.WGL;
using Tensorflow.Keras.Layers;



#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


namespace XrEditor
{
    public class GlRenderHost : RenderHost, IOpenGLDevice, IXrGraphicProvider, INativeContext
    {
        protected HwndSource? _hwndSource;
        protected GL? _gl;
        protected readonly WGL _wgl;
        protected readonly bool _createContext;
        protected nint _glCtx;
        protected nint _hdc;
        protected bool _useEs;
        protected static wglCreateContextAttribsARBPtr? CreateContextAttribsARB;
        protected static wglSwapIntervalEXTPtr? SwapIntervalEXT;

        #region NATIVE

        protected delegate bool wglSwapIntervalEXTPtr(int interval);

        protected unsafe delegate nint wglCreateContextAttribsARBPtr(nint hDC, nint hshareContext, int* attribList);

        [DllImport("User32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern int SwapBuffers(IntPtr hDC);

        const byte PFD_TYPE_RGBA = 0;
        const byte PFD_TYPE_COLORINDEX = 1;

        const uint PFD_DOUBLEBUFFER = 1;
        const uint PFD_STEREO = 2;
        const uint PFD_DRAW_TO_WINDOW = 4;
        const uint PFD_DRAW_TO_BITMAP = 8;
        const uint PFD_SUPPORT_GDI = 16;
        const uint PFD_SUPPORT_OPENGL = 32;
        const uint PFD_GENERIC_FORMAT = 64;
        const uint PFD_NEED_PALETTE = 128;
        const uint PFD_NEED_SYSTEM_PALETTE = 256;
        const uint PFD_SWAP_EXCHANGE = 512;
        const uint PFD_SWAP_COPY = 1024;
        const uint PFD_SWAP_LAYER_BUFFERS = 2048;
        const uint PFD_GENERIC_ACCELERATED = 4096;
        const uint PFD_SUPPORT_DIRECTDRAW = 8192;
        const uint PFD_DIRECT3D_ACCELERATED = 0x00004000;
        const uint PFD_SUPPORT_COMPOSITION = 0x00008000;

        const sbyte PFD_MAIN_PLANE = 0;
        const sbyte PFD_OVERLAY_PLANE = 1;
        const sbyte PFD_UNDERLAY_PLANE = -1;

        const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
        const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
        const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
        const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;
        const int WGL_CONTEXT_ES_PROFILE_BIT_EXT = 0x00000004;
        const int WGL_SAMPLE_BUFFERS_ARB = 0x2041;
        const int WGL_SAMPLES_ARB = 0x2042;

        #endregion

        public GlRenderHost(bool createContext = true, bool useEs = false)
        {
            _wgl = WGL.GetApi();

            _createContext = createContext;
            _useEs = useEs;
        }

        protected unsafe virtual void CreateContext(HandleRef handle)
        {
            var pfd = new PixelFormatDescriptor
            {
                NSize = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
                NVersion = 1,
                IPixelType = PFD_TYPE_RGBA,
                DwFlags = PFD_SUPPORT_OPENGL | PFD_SUPPORT_COMPOSITION | PFD_DIRECT3D_ACCELERATED | PFD_DRAW_TO_WINDOW | PFD_DOUBLEBUFFER,
                ILayerType = (byte)PFD_MAIN_PLANE,
                CColorBits = 24,
                CAlphaBits = 8,
                CDepthBits = 24,
                CStencilBits = 8
            };

            DefaultNativeContext.TryCreate("Opengl32.dll", out var opengl);
            DefaultNativeContext.TryCreate("Gdi32.dll", out var gdi);

            var wgl = new WGL(new MultiNativeContext(gdi,opengl));

            _hdc = GetDC(handle.Handle);

            var pfIndex = wgl.ChoosePixelFormat(_hdc, ref pfd);
            if (pfIndex <= 0)
                throw new Win32Exception();

            if (!wgl.SetPixelFormat(_hdc, pfIndex, ref pfd))
                throw new Win32Exception();

            _glCtx = wgl.CreateContext(_hdc);

            if (_glCtx == IntPtr.Zero)
                throw new Win32Exception();

            TakeContext();

            // EnableVSync(false);

            CreateContextAttribsARB = Marshal.GetDelegateForFunctionPointer
                <wglCreateContextAttribsARBPtr>(wgl.GetProcAddress("wglCreateContextAttribsARB"));

            var attr = stackalloc int[11];

            if (!_useEs)
            {
                attr[0] = WGL_CONTEXT_MAJOR_VERSION_ARB;
                attr[1] = 4;

                attr[2] = WGL_CONTEXT_MINOR_VERSION_ARB;
                attr[3] = 6;

                attr[4] = WGL_CONTEXT_PROFILE_MASK_ARB;
                attr[5] = WGL_CONTEXT_CORE_PROFILE_BIT_ARB;
            }
            else
            {
                attr[0] = WGL_CONTEXT_MAJOR_VERSION_ARB;
                attr[1] = 3;

                attr[2] = WGL_CONTEXT_MINOR_VERSION_ARB;
                attr[3] = 2;

                attr[4] = WGL_CONTEXT_PROFILE_MASK_ARB;
                attr[5] = WGL_CONTEXT_ES_PROFILE_BIT_EXT;
            }

            attr[6] = 0;
            /*
            attr[6] = WGL_SAMPLE_BUFFERS_ARB;
            attr[7] = 1;
            
            attr[8] = WGL_SAMPLES_ARB;
            attr[9] = 4;
            */
            _glCtx = CreateContextAttribsARB(_hdc, _glCtx, attr);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var handle = base.BuildWindowCore(hwndParent);

            if (_createContext)
                CreateContext(handle);

#if GL_WRAPPER
            _gl = new OpenGLWrapper.GlSwitchWrapper(Silk.NET.OpenGL.GL.GetApi(this));
#else
            _gl = GL.GetApi(this);
#endif
            return handle;
        }

        public override IRenderEngine CreateRenderEngine(object? driverOptions)
        {
            var glOptions = driverOptions as GlRenderOptions ?? new GlRenderOptions();

            glOptions.FloatPrecision = ShaderPrecision.High;
            glOptions.Outline.Use = true;

            var render = new OpenGLRender(_gl!, glOptions);

            TakeContext();

            render.EnableDebug(EditorDebug.DebugSync);

            return render;
        }


        public override void EnableVSync(bool enable, int scale = 1)
        {
            SwapIntervalEXT ??= Marshal.GetDelegateForFunctionPointer
                    <wglSwapIntervalEXTPtr>(_wgl.GetProcAddress("wglSwapIntervalEXT"));

            var res = SwapIntervalEXT(enable ? scale : 0);
        }

        public override void SwapBuffers()
        {
           SwapBuffers(_hdc);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_glCtx != 0)
            {
                _wgl.DeleteContext(_glCtx);
                _glCtx = 0;
            }

            if (_hwndSource != null)
                _ = ReleaseDC(_hwndSource.Handle, _hdc);

            base.DestroyWindowCore(hwnd);
        }


        public override void ReleaseContext()
        {
            if (_hdc == 0)
                return;

            if (!_wgl.MakeCurrent(_hdc, IntPtr.Zero))
                throw new Win32Exception();
        }

        public override bool TakeContext()
        {
            if (!_wgl.MakeCurrent(_hdc, _glCtx))
                throw new Win32Exception();
            return true;
        }

        public IXrGraphicDriver CreateXrDriver()
        {
            return new XrOpenGLGraphicDriver(this);
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = GetProcAddress(proc);
            return addr != IntPtr.Zero;
        }

        public nint GetProcAddress(string proc, int? slot = null)
        {
            var addr = _wgl.Context.GetProcAddress(proc);
            if (addr == IntPtr.Zero)
                addr = _wgl.GetProcAddress(proc);
            return addr;
        }

        public GL Gl => _gl ?? throw new NullReferenceException();

        public nint HDc => _hdc;

        public nint GlCtx => _glCtx;

        public override bool SupportsDualRender => true;
    }
}

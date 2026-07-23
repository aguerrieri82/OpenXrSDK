using Silk.NET.Core.Contexts;
using Silk.NET.WGL;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;


#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL.Wpf
{
    public class GlRenderHost : RenderHost, INativeContext, IGlContextProvider
    {
        protected HwndSource? _hwndSource;
        protected GL? _gl;
        protected readonly WGL _wgl;
        protected readonly bool _createContext;
        protected nint _glCtx;
        protected nint _hdc;
        protected bool _useEs;
        protected WinGlContext? _mainCtx;

        protected static wglCreateContextAttribsARBPtr? CreateContextAttribsARB;
        protected static wglChoosePixelFormatARBPtr? ChoosePixelFormatARB;
        protected static wglSwapIntervalEXTPtr? SwapIntervalEXT;
        protected static wglGetPixelFormatAttribivARBPtr? GetPixelFormatAttribivARB;
        private OpenGLRender? _render;

        #region Native

        protected delegate bool wglSwapIntervalEXTPtr(int interval);

        protected delegate nint wglCreateContextAttribsARBPtr(
            nint hDC,
            nint hshareContext,
            int[] attribList);

        protected delegate bool wglChoosePixelFormatARBPtr(
            nint hdc,
            int[] piAttribIList,
            float[]? pfAttribFList,
            uint nMaxFormats,
            out int piFormats,
            out uint nNumFormats);

        protected delegate bool wglGetPixelFormatAttribivARBPtr(
            nint hdc,
            int iPixelFormat,
            int iLayerPlane,
            uint nAttributes,
            int[] piAttributes,
            int[] piValues);

        private delegate nint WndProc2(nint hWnd, uint msg, nint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASS
        {
            public uint style;
            public WndProc2 lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
        }

        private const int ERROR_CLASS_ALREADY_EXISTS = 1410;

        private const int CS_OWNDC = 0x0020;
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const int WS_OVERLAPPEDWINDOW = 0x00CF0000;

        private const byte PFD_TYPE_RGBA = 0;
        private const byte PFD_MAIN_PLANE = 0;

        private const uint PFD_DOUBLEBUFFER = 0x00000001;
        private const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        private const uint PFD_SUPPORT_OPENGL = 0x00000020;
        private const uint PFD_DIRECT3D_ACCELERATED = 0x00004000;
        private const uint PFD_SUPPORT_COMPOSITION = 0x00008000;

        private const int WGL_DRAW_TO_WINDOW_ARB = 0x2001;
        private const int WGL_SUPPORT_OPENGL_ARB = 0x2010;
        private const int WGL_DOUBLE_BUFFER_ARB = 0x2011;
        private const int WGL_PIXEL_TYPE_ARB = 0x2013;
        private const int WGL_TYPE_RGBA_ARB = 0x202B;

        private const int WGL_COLOR_BITS_ARB = 0x2014;
        private const int WGL_ALPHA_BITS_ARB = 0x201B;
        private const int WGL_DEPTH_BITS_ARB = 0x2022;
        private const int WGL_STENCIL_BITS_ARB = 0x2023;
        private const int WGL_FRAMEBUFFER_SRGB_CAPABLE_ARB = 0x20A9;

        private const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
        private const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
        private const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
        private const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;
        private const int WGL_CONTEXT_ES_PROFILE_BIT_EXT = 0x00000004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint CreateWindowExW(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            nint hWndParent,
            nint hMenu,
            nint hInstance,
            nint lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint GetDC(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(nint hWnd, nint hDC);

        [DllImport("gdi32.dll")]
        private static extern int SwapBuffers(nint hDC);

        #endregion

        private static readonly WndProc2 DummyWndProc = DefWindowProcW;

        public GlRenderHost(bool createContext = true, bool useEs = false)
        {
            Context.Implement<IGlContextProvider>(this);

            DefaultNativeContext.TryCreate("Opengl32.dll", out var opengl);
            DefaultNativeContext.TryCreate("Gdi32.dll", out var gdi);

            _wgl = new WGL(new MultiNativeContext(gdi, opengl));

            _createContext = createContext;
            _useEs = useEs;
        }

        protected virtual void CreateContext(nint hWnd)
        {
            CreateDummyWglContext(out var dummyWnd, out var dummyHdc, out var dummyCtx);

            LoadWglExtensions();

            DestroyDummyWglContext(dummyWnd, dummyHdc, dummyCtx);

            _hdc = GetDC(hWnd);

            if (_hdc == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var pfd = CreatePixelFormatDescriptor();

            int[] pfAttribs =
            [
                WGL_DRAW_TO_WINDOW_ARB, 1,
                    WGL_SUPPORT_OPENGL_ARB, 1,
                    WGL_DOUBLE_BUFFER_ARB, 1,
                    WGL_PIXEL_TYPE_ARB, WGL_TYPE_RGBA_ARB,

                    WGL_COLOR_BITS_ARB, 24,
                    WGL_ALPHA_BITS_ARB, 8,
                    WGL_DEPTH_BITS_ARB, 24,
                    WGL_STENCIL_BITS_ARB, 8,

                  //  WGL_FRAMEBUFFER_SRGB_CAPABLE_ARB, 1,

                    0
            ];

            if (!ChoosePixelFormatARB!(_hdc, pfAttribs, null, 1, out var pixelFormat, out var numFormats) ||
                numFormats == 0)
            {
                throw new NotSupportedException("No sRGB-capable WGL pixel format found.");
            }

            if (!_wgl.SetPixelFormat(_hdc, pixelFormat, ref pfd))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            _glCtx = CreateContextWork();

        }

        protected nint CreateContextWork()
        {
            int[] attr;

            if (!_useEs)
            {
                attr = [
                    WGL_CONTEXT_MAJOR_VERSION_ARB, 4,
                    WGL_CONTEXT_MINOR_VERSION_ARB, 6,
                    WGL_CONTEXT_PROFILE_MASK_ARB, WGL_CONTEXT_CORE_PROFILE_BIT_ARB,
                    0,
                ];
            }
            else
            {
                attr = [
                    WGL_CONTEXT_MAJOR_VERSION_ARB, 3,
                    WGL_CONTEXT_MINOR_VERSION_ARB, 2,
                    WGL_CONTEXT_PROFILE_MASK_ARB, WGL_CONTEXT_ES_PROFILE_BIT_EXT,
                    0,
                ];
            }

            var result = CreateContextAttribsARB!(_hdc, 0, attr);

            if (result == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return result;
        }

        private void LoadWglExtensions()
        {
            ChoosePixelFormatARB = Marshal.GetDelegateForFunctionPointer
                <wglChoosePixelFormatARBPtr>(_wgl.GetProcAddress("wglChoosePixelFormatARB"));

            CreateContextAttribsARB = Marshal.GetDelegateForFunctionPointer
                <wglCreateContextAttribsARBPtr>(_wgl.GetProcAddress("wglCreateContextAttribsARB"));

            GetPixelFormatAttribivARB = Marshal.GetDelegateForFunctionPointer
                <wglGetPixelFormatAttribivARBPtr>(
                    _wgl.GetProcAddress("wglGetPixelFormatAttribivARB"));
        }

        private static PixelFormatDescriptor CreatePixelFormatDescriptor()
        {
            return new PixelFormatDescriptor
            {
                NSize = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),
                NVersion = 1,
                IPixelType = PFD_TYPE_RGBA,
                DwFlags =
                    PFD_SUPPORT_OPENGL |
                    PFD_SUPPORT_COMPOSITION |
                    PFD_DIRECT3D_ACCELERATED |
                    PFD_DRAW_TO_WINDOW |
                    PFD_DOUBLEBUFFER,
                ILayerType = PFD_MAIN_PLANE,
                CColorBits = 24,
                CAlphaBits = 8,
                CDepthBits = 24,
                CStencilBits = 8
            };
        }

        private void CreateDummyWglContext(
            out nint dummyWnd,
            out nint dummyHdc,
            out nint dummyCtx)
        {
            var hInstance = GetModuleHandle(null);
            var className = "XrEngineDummyWglWindow";

            var wc = new WNDCLASS
            {
                style = CS_OWNDC,
                lpfnWndProc = DummyWndProc,
                hInstance = hInstance,
                lpszClassName = className
            };

            var atom = RegisterClassW(ref wc);

            if (atom == 0)
            {
                var error = Marshal.GetLastWin32Error();

                if (error != ERROR_CLASS_ALREADY_EXISTS)
                    throw new Win32Exception(error);
            }

            dummyWnd = CreateWindowExW(
                0,
                className,
                "Dummy WGL",
                WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                1,
                1,
                0,
                0,
                hInstance,
                0);

            if (dummyWnd == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            dummyHdc = GetDC(dummyWnd);

            if (dummyHdc == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var pfd = CreatePixelFormatDescriptor();

            var pixelFormat = _wgl.ChoosePixelFormat(dummyHdc, ref pfd);

            if (pixelFormat <= 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!_wgl.SetPixelFormat(dummyHdc, pixelFormat, ref pfd))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            dummyCtx = _wgl.CreateContext(dummyHdc);

            if (dummyCtx == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!_wgl.MakeCurrent(dummyHdc, dummyCtx))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        private void DestroyDummyWglContext(
            nint dummyWnd,
            nint dummyHdc,
            nint dummyCtx)
        {
            _wgl.MakeCurrent(0, 0);

            if (dummyCtx != 0)
                _wgl.DeleteContext(dummyCtx);

            if (dummyHdc != 0 && dummyWnd != 0)
                ReleaseDC(dummyWnd, dummyHdc);

            if (dummyWnd != 0)
                DestroyWindow(dummyWnd);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var handle = base.BuildWindowCore(hwndParent);

            if (_createContext)
                CreateContext(handle.Handle);

#if GL_WRAPPER
            _gl = new OpenGLWrapper.GlSwitchWrapper(Silk.NET.OpenGL.GL.GetApi(this));
#else
            _gl = GL.GetApi(this);
#endif

            _mainCtx = WinGlContext.Attach(_hdc, _glCtx, _gl, _wgl);

            _mainCtx.Take();

            return handle;
        }

        public override IRenderEngine CreateRenderEngine(object? driverOptions)
        {
            var glOptions = driverOptions as GlRenderOptions ?? new GlRenderOptions();

            glOptions.FloatPrecision = ShaderPrecision.High;
            glOptions.Outline.Use = true;

            _render = new OpenGLRender(_gl!, glOptions);

            TakeContext();

            return _render;
        }

        public override void EnableVSync(bool enable, int scale = 1)
        {
            SwapIntervalEXT ??= Marshal.GetDelegateForFunctionPointer
                <wglSwapIntervalEXTPtr>(_wgl.GetProcAddress("wglSwapIntervalEXT"));

            _ = SwapIntervalEXT(enable ? scale : 0);
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
            _mainCtx?.Release();
        }

        public override bool TakeContext()
        {
            _mainCtx?.Take();
            return true;
        }


        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = GetProcAddress(proc);
            return addr != 0;
        }

        public override void BeginFrame(long frameNum)
        {
            /*
            GlState.Current.EnableFeature(EnableCap.FramebufferSrgb, true);

            GlState.Current.BindFrameBuffer(FramebufferTarget.Framebuffer, 0);
            _gl.GetFramebufferAttachmentParameter(
                FramebufferTarget.Framebuffer,
                GLEnum.BackLeft,
                FramebufferAttachmentParameterName.ColorEncoding,
                out var encoding);

            bool isSrgb = encoding == (int)InternalFormat.Srgb;

            */

            _render!.SetRenderTarget((IGlRenderTarget?)null);
            _render!.PushGroup($"Begin frame {frameNum}");
        }

        public override void EndFrame()
        {
            _render!.PopGroup();
        }

        public nint GetProcAddress(string proc, int? slot = null)
        {
            _wgl.Context.TryGetProcAddress(proc, out var addr);

            if (addr == 0)
                addr = _wgl.GetProcAddress(proc);

            return addr;
        }

        public IGlContext CreateShared()
        {
            Debug.Assert(_gl != null);

            var newCtx = CreateContextWork();

            if (!_wgl.ShareLists(_glCtx, newCtx))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return WinGlContext.Attach(_hdc, newCtx, _gl, _wgl);
        }

        IGlContext? IGlContextProvider.Current => WinGlContext.Current;

        public GL Gl => _gl ?? throw new NullReferenceException();

        public nint HDc => _hdc;

        public nint GlCtx => _glCtx;

        public override bool SupportsDualRender => true;
    }
}
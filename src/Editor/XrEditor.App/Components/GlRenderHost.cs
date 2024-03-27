using OpenXr.Framework;
using OpenXr.Framework.OpenGL;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using XrEngine;
using XrEngine.OpenGL;



namespace XrEditor
{
    public class GlRenderHost : RenderHost, INativeContext, IOpenGLDevice, IXrGraphicProvider
    {
        protected HwndSource? _hwndSource;
        protected GL? _gl;
        protected readonly nint _hLib;
        private readonly bool _createContext;
        protected nint _glCtx;
        protected nint _hdc;

        #region NATIVE

        protected delegate bool wglSwapIntervalEXTPtr(int interval);

        protected unsafe delegate nint wglCreateContextAttribsARBPtr(nint hDC, nint hshareContext, int* attribList);

        protected static wglSwapIntervalEXTPtr? wglSwapIntervalEXT;

        protected static wglCreateContextAttribsARBPtr? wglCreateContextAttribsARB;

        [DllImport("Opengl32.dll")]
        static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("Opengl32.dll")]
        static extern IntPtr wglDeleteContext(IntPtr hglrc);

        [DllImport("Opengl32.dll", SetLastError = true)]
        static extern IntPtr wglChoosePixelFormatARB(IntPtr hdc);

        [DllImport("Opengl32.dll", SetLastError = true)]
        static extern IntPtr wglGetProcAddress([MarshalAs(UnmanagedType.LPStr)] string unnamedParam1);

        [DllImport("Opengl32.dll", SetLastError = true)]
        static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern int ChoosePixelFormat(IntPtr hDC,
        [In, MarshalAs(UnmanagedType.LPStruct)] PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern bool SetPixelFormat(IntPtr hDC, int iPixelFormat,
            [In, MarshalAs(UnmanagedType.LPStruct)] PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll")]
        static extern int SwapBuffers(IntPtr hDC);

        [DllImport("User32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);


        [DllImport("User32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);


        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

        [StructLayout(LayoutKind.Explicit)]
        class PIXELFORMATDESCRIPTOR
        {
            [FieldOffset(0)]
            public UInt16 nSize;
            [FieldOffset(2)]
            public UInt16 nVersion;
            [FieldOffset(4)]
            public UInt32 dwFlags;
            [FieldOffset(8)]
            public Byte iPixelType;
            [FieldOffset(9)]
            public Byte cColorBits;
            [FieldOffset(10)]
            public Byte cRedBits;
            [FieldOffset(11)]
            public Byte cRedShift;
            [FieldOffset(12)]
            public Byte cGreenBits;
            [FieldOffset(13)]
            public Byte cGreenShift;
            [FieldOffset(14)]
            public Byte cBlueBits;
            [FieldOffset(15)]
            public Byte cBlueShift;
            [FieldOffset(16)]
            public Byte cAlphaBits;
            [FieldOffset(17)]
            public Byte cAlphaShift;
            [FieldOffset(18)]
            public Byte cAccumBits;
            [FieldOffset(19)]
            public Byte cAccumRedBits;
            [FieldOffset(20)]
            public Byte cAccumGreenBits;
            [FieldOffset(21)]
            public Byte cAccumBlueBits;
            [FieldOffset(22)]
            public Byte cAccumAlphaBits;
            [FieldOffset(23)]
            public Byte cDepthBits;
            [FieldOffset(24)]
            public Byte cStencilBits;
            [FieldOffset(25)]
            public Byte cAuxBuffers;
            [FieldOffset(26)]
            public SByte iLayerType;
            [FieldOffset(27)]
            public Byte bReserved;
            [FieldOffset(28)]
            public UInt32 dwLayerMask;
            [FieldOffset(32)]
            public UInt32 dwVisibleMask;
            [FieldOffset(36)]
            public UInt32 dwDamageMask;
        }

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

        public GlRenderHost(bool createContext = true)
        {
            _hLib = LoadLibraryW("opengl32.dll");
            _createContext = createContext;
        }

        protected unsafe virtual void CreateContext(HandleRef handle)
        {
            var pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                iPixelType = PFD_TYPE_RGBA,
                dwFlags = PFD_SUPPORT_OPENGL | PFD_SUPPORT_COMPOSITION | PFD_DIRECT3D_ACCELERATED | PFD_DRAW_TO_WINDOW | PFD_DOUBLEBUFFER,
                iLayerType = PFD_MAIN_PLANE,
                cColorBits = 24,
                cAlphaBits = 8,
                cDepthBits = 24,
               
                cStencilBits = 8
            };

            _hdc = GetDC(handle.Handle);

            var pfIndex = ChoosePixelFormat(_hdc, pfd);
            if (pfIndex <= 0)
                throw new Win32Exception();

            if (!SetPixelFormat(_hdc, pfIndex, pfd))
                throw new Win32Exception();

            _glCtx = wglCreateContext(_hdc);

            if (_glCtx == IntPtr.Zero)
                throw new Win32Exception();

            TakeContext();

           // EnableVSync(false);

            var pointer = GetProcAddress("wglCreateContextAttribsARB");
            wglCreateContextAttribsARB = Marshal.GetDelegateForFunctionPointer<wglCreateContextAttribsARBPtr>(pointer);

            var attr = stackalloc int[11];

            attr[0] = WGL_CONTEXT_MAJOR_VERSION_ARB;
            attr[1] = 4;

            attr[2] = WGL_CONTEXT_MINOR_VERSION_ARB;
            attr[3] = 6;
            
            attr[4] = WGL_CONTEXT_PROFILE_MASK_ARB;
            attr[5] = WGL_CONTEXT_CORE_PROFILE_BIT_ARB;
           //attr[5] = WGL_CONTEXT_ES_PROFILE_BIT_EXT; ;

            attr[6] = 0;

            /*
            attr[6] = WGL_SAMPLE_BUFFERS_ARB;
            attr[7] = 1;
            
            attr[8] = WGL_SAMPLES_ARB;
            attr[9] = 4;
            */


            _glCtx = wglCreateContextAttribsARB(_hdc, _glCtx, attr);

        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var handle = base.BuildWindowCore(hwndParent);

            if (_createContext)
                CreateContext(handle);

            _gl = GL.GetApi(this);

            return handle;
        }

        public override IRenderEngine CreateRenderEngine()
        {
            var render = new OpenGLRender(_gl!, new GlRenderOptions
            {
                FloatPrecision = ShaderPrecision.High,
                ShaderVersion = "300 es",
                RequireTextureCompression = false
            });

            TakeContext();

            render.EnableDebug();

            return render;
        }


        public override void EnableVSync(bool enable)
        {
            if (wglSwapIntervalEXT == null)
            {
                var addr = GetProcAddress("wglSwapIntervalEXT");
                wglSwapIntervalEXT = Marshal.GetDelegateForFunctionPointer<wglSwapIntervalEXTPtr>(addr);
            }

            var res = wglSwapIntervalEXT(enable ? 1 : 0);
        }

        public override void SwapBuffers()
        {
            _ = SwapBuffers(_hdc);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_glCtx != 0)
            {
                wglDeleteContext(_glCtx);
                _glCtx = 0;
            }

            if (_hwndSource != null)
                _ = ReleaseDC(_hwndSource.Handle, _hdc);

            base.DestroyWindowCore(hwnd);
        }

        public nint GetProcAddress(string proc, int? slot = null)
        {
            var addr = GetProcAddress(_hLib, proc);

            if (addr == 0)
            {
                addr = wglGetProcAddress(proc);
                /*
                if (addr == 0)
                    throw new NotSupportedException(proc);
                */
            }

            return addr;
        }

        public override void ReleaseContext()
        {
            if (_hdc == 0)
                return;

            if (!wglMakeCurrent(_hdc, IntPtr.Zero))
                throw new Win32Exception();
        }

        public override bool TakeContext()
        {
            if (!wglMakeCurrent(_hdc, _glCtx))
                throw new Win32Exception();
            return true;
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = wglGetProcAddress(proc);
            return addr != 0;
        }

        public IXrGraphicDriver CreateXrDriver()
        {
            return new XrOpenGLGraphicDriver(this);
        }

        public GL? Gl => _gl;

        public nint HDc => _hdc;

        public nint GlCtx => _glCtx;

        public override bool SupportsDualRender => true;
    }
}

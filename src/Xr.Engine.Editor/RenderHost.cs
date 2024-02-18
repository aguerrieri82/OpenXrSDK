#if GLES
using Silk.NET.OpenGLES;
#else

using Silk.NET.OpenGL;
#endif

using Silk.NET.Core.Contexts;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenXr.Framework;


namespace Xr.Engine.Editor
{

    public class RenderHost : HwndHost, INativeContext
    {
        private HwndSource? _hwndSource;
        private GL? _gl;
        private nint _hLib;
        private nint _glCtx;
        private nint _hdc;

        [DllImport("Opengl32.dll")]
        static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("Opengl32.dll")]
        static extern IntPtr wglChoosePixelFormatARB(IntPtr hdc);

        [DllImport("Opengl32.dll")]
        static extern IntPtr wglGetProcAddress([MarshalAs(UnmanagedType.LPStr)]string unnamedParam1);

        [DllImport("Opengl32.dll")]
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
        static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)]string procName);

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



        protected unsafe override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            Gpu.EnableNvAPi();

            if (DesignerProperties.GetIsInDesignMode(this))
                return new HandleRef(null, 0);

            _hwndSource = new HwndSource(0, 0x40000000, 0, 0, 0, "", hwndParent.Handle);

            var handle = _hwndSource.CreateHandleRef();

            _hLib = LoadLibraryW("opengl32.dll");

            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR();
            pfd.nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>();
            pfd.nVersion = 1;
            pfd.iPixelType = PFD_TYPE_RGBA;
            pfd.dwFlags = PFD_SUPPORT_OPENGL | PFD_SUPPORT_COMPOSITION | PFD_DIRECT3D_ACCELERATED | PFD_DRAW_TO_WINDOW | PFD_DOUBLEBUFFER;
            pfd.iLayerType = PFD_MAIN_PLANE;
            pfd.cColorBits = 24;
            pfd.cDepthBits = 32;

            _hdc = GetDC(handle.Handle);

            var pfIndex = ChoosePixelFormat(_hdc, pfd);
            SetPixelFormat(_hdc, pfIndex, pfd);
            _glCtx = wglCreateContext(_hdc);

            wglMakeCurrent(_hdc, _glCtx);

            //ReleaseDC(_hwndSource.Handle, _hdc);

            _gl = GL.GetApi(this);

            return handle;
        }


        public void SwapBuffers()
        {
            SwapBuffers(_hdc);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            _hwndSource?.Dispose();
        }

        public nint GetProcAddress(string proc, int? slot = null)
        {
            var addr = GetProcAddress(_hLib, proc);

            if (addr == 0)
            {
                addr = wglGetProcAddress(proc);
                if (addr == 0)
                    throw new NotSupportedException(proc); 
            }

            return addr;
        }

        public void ReleaseContext()
        {
            wglMakeCurrent(_hdc, IntPtr.Zero);
        }

        public void TakeContext()
        {
            wglMakeCurrent(_hdc, _glCtx);
        }

        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
        {
            addr = wglGetProcAddress(proc);
            return addr != 0;
        }

        public GL? Gl => _gl;
    }
}

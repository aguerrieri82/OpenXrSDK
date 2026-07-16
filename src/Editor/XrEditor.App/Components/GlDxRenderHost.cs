using OpenXr.Framework;
using OpenXr.Framework.OpenGL;
using Silk.NET.Core.Contexts;
using Silk.NET.WGL;
using Silk.NET.WGL.Extensions.NV;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using XrEngine;
using XrEngine.OpenGL;
using OpenTK.Graphics.Wgl;
using System.Windows.Threading;
using System.Windows.Media;
using static XrEngine.Filament.FilamentLib;




#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEditor;

public unsafe class GlDxRenderHost : ImageRenderHost,
    IOpenGLDevice,
    IXrGraphicProvider,
    INativeContext,
    IGlContextProvider,
    IDisposable
{
    protected GL? _gl;
    protected readonly WGL _wgl;
    protected readonly bool _createContext;
    protected nint _glCtx;
    protected nint _hdc;
    protected bool _useEs;
    protected WinGlContext? _mainCtx;

    private nint _hiddenWnd;

    private D3DImage? _image;
    private nint _d3d;
    private nint _d3dDevice;
    private nint _d3dTexture;
    private nint _d3dSurface;

    private NVDXInterop? _dxInterop;
    private nint _interopDevice;
    private nint _interopObject;

    private int _targetWidth;
    private int _targetHeight;
    private int _invalidatePending;

    private GlTexture? _colorTex;
    private GlTexture? _interopTex;
    private GlRenderBuffer? _depthBuffer;
    private GlTextureRenderTarget? _renderTarget;
    private GlTextureFrameBuffer? _interopFrameBuffer;
    private OpenGLRender? _render;

    private static readonly object WglBootstrapLock = new();
    private static bool _wglExtensionsLoaded;

    private readonly AutoResetEvent _compositionFrame = new(false);
    private bool _vsyncEnabled;
    private int _vsyncScale = 1;
    private int _compositionFrameIndex;


    #region WGL interop

    protected static wglCreateContextAttribsARBPtr? CreateContextAttribsARB;
    protected static wglChoosePixelFormatARBPtr? ChoosePixelFormatARB;
    protected static wglSwapIntervalEXTPtr? SwapIntervalEXT;
    protected static wglGetPixelFormatAttribivARBPtr? GetPixelFormatAttribivARB;

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

    private delegate nint WndProc2(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam);

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

    [DllImport(
        "user32.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

    [DllImport(
        "user32.dll",
        SetLastError = true,
        CharSet = CharSet.Unicode)]
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
    private static extern nint DefWindowProcW(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll")]
    private static extern int SwapBuffers(nint hDC);

    private static readonly WndProc2 DummyWndProc = DefWindowProcW;

    #endregion

    #region D3D9 interop

    private const uint D3DSdkVersion = 32;
    private const uint D3DAdapterDefault = 0;

    private const uint D3DCreateFpuPreserve = 0x00000002;
    private const uint D3DCreateMultithreaded = 0x00000004;
    private const uint D3DCreateHardwareVertexProcessing = 0x00000040;

    private const uint D3DUsageRenderTarget = 0x00000001;
    private const uint D3DPresentIntervalImmediate = 0x80000000;

    private const int D3DDevTypeHal = 1;
    private const int D3DFormatUnknown = 0;
    private const int D3DFormatA8R8G8B8 = 21;
    private const int D3DPoolDefault = 0;
    private const int D3DSwapEffectDiscard = 1;

    [DllImport("d3d9.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int Direct3DCreate9Ex(
        uint sdkVersion,
        out nint direct3D9Ex);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseDelegate(nint self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateDeviceExDelegate(
        nint self,
        uint adapter,
        int deviceType,
        nint focusWindow,
        uint behaviorFlags,
        ref D3DPresentParameters presentationParameters,
        void* fullscreenDisplayMode,
        out nint returnedDeviceInterface);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateTextureDelegate(
        nint self,
        uint width,
        uint height,
        uint levels,
        uint usage,
        int format,
        int pool,
        out nint texture,
        nint* sharedHandle);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetSurfaceLevelDelegate(
        nint self,
        uint level,
        out nint surface);

    [StructLayout(LayoutKind.Sequential)]
    private struct D3DPresentParameters
    {
        public uint BackBufferWidth;
        public uint BackBufferHeight;
        public int BackBufferFormat;
        public uint BackBufferCount;
        public int MultiSampleType;
        public uint MultiSampleQuality;
        public int SwapEffect;
        public nint DeviceWindow;
        public int Windowed;
        public int EnableAutoDepthStencil;
        public int AutoDepthStencilFormat;
        public uint Flags;
        public uint FullScreenRefreshRateInHz;
        public uint PresentationInterval;
    }

    #endregion

    public GlDxRenderHost(bool createContext = true, bool useEs = false)
    {
        Context.Implement<IGlContextProvider>(this);

        DefaultNativeContext.TryCreate("Opengl32.dll", out var opengl);
        DefaultNativeContext.TryCreate("Gdi32.dll", out var gdi);

        _wgl = new WGL(new MultiNativeContext(gdi, opengl));
        _createContext = createContext;
        _useEs = useEs;
    }

    protected override void OnHostLoaded()
    {
        base.OnHostLoaded();

        CompositionTarget.Rendering += OnCompositionRendering;

        if (!_createContext || _glCtx != 0)
            return;

        EnsureWglExtensionsLoaded();

        _hiddenWnd = CreateHiddenWindow();
        CreateContext(_hiddenWnd);

#if GL_WRAPPER
        _gl = new OpenGLWrapper.GlSwitchWrapper(
            Silk.NET.OpenGL.GL.GetApi(this));
#else
        _gl = GL.GetApi(this);
#endif

        _mainCtx = WinGlContext.Attach(_hdc, _glCtx, _gl, _wgl);
        _mainCtx.Take();

        if (!WGL.GetApi().TryGetExtension(out _dxInterop))
            throw new NotSupportedException(
                "WGL_NV_DX_interop is not available.");

        _image = new D3DImage();
        Source = _image;

        CreateD3DDevice();

        _interopDevice = _dxInterop!.DxopenDevice((void*)_d3dDevice);

        if (_interopDevice == 0)
        {
            throw new InvalidOperationException(
                "wglDXOpenDeviceNV failed for the D3D9Ex device.");
        }

        RecreateTarget();
    }

    protected override void OnHostUnloaded()
    {
        CompositionTarget.Rendering -= OnCompositionRendering;
        _compositionFrame.Set();

        DisposePresentationTarget();
        base.OnHostUnloaded();
    }

    protected virtual void CreateContext(nint hWnd)
    {
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
            WGL_FRAMEBUFFER_SRGB_CAPABLE_ARB, 1,
            0
        ];

        if (!ChoosePixelFormatARB!(
                _hdc,
                pfAttribs,
                null,
                1,
                out var pixelFormat,
                out var numFormats) ||
            numFormats == 0)
        {
            throw new NotSupportedException(
                "No sRGB-capable WGL pixel format found.");
        }

        if (!_wgl.SetPixelFormat(_hdc, pixelFormat, ref pfd))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        _glCtx = CreateContextWork();
    }

    protected nint CreateContextWork()
    {
        int[] attributes = !_useEs
            ?
            [
                WGL_CONTEXT_MAJOR_VERSION_ARB, 4,
                WGL_CONTEXT_MINOR_VERSION_ARB, 6,
                WGL_CONTEXT_PROFILE_MASK_ARB,
                WGL_CONTEXT_CORE_PROFILE_BIT_ARB,
                0
            ]
            :
            [
                WGL_CONTEXT_MAJOR_VERSION_ARB, 3,
                WGL_CONTEXT_MINOR_VERSION_ARB, 2,
                WGL_CONTEXT_PROFILE_MASK_ARB,
                WGL_CONTEXT_ES_PROFILE_BIT_EXT,
                0
            ];

        nint result = CreateContextAttribsARB!(_hdc, 0, attributes);

        if (result == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return result;
    }

    private void CreateD3DDevice()
    {
        int hr = Direct3DCreate9Ex(D3DSdkVersion, out _d3d);
        ThrowIfFailed(hr, nameof(Direct3DCreate9Ex));

        var parameters = new D3DPresentParameters
        {
            BackBufferWidth = 1,
            BackBufferHeight = 1,
            BackBufferFormat = D3DFormatUnknown,
            BackBufferCount = 1,
            SwapEffect = D3DSwapEffectDiscard,
            DeviceWindow = _hiddenWnd,
            Windowed = 1,
            PresentationInterval = D3DPresentIntervalImmediate
        };

        var createDeviceEx =
            GetComMethod<CreateDeviceExDelegate>(_d3d, 20);

        hr = createDeviceEx(
            _d3d,
            D3DAdapterDefault,
            D3DDevTypeHal,
            _hiddenWnd,
            D3DCreateFpuPreserve |
            D3DCreateMultithreaded |
            D3DCreateHardwareVertexProcessing,
            ref parameters,
            null,
            out _d3dDevice);

        ThrowIfFailed(hr, "IDirect3D9Ex::CreateDeviceEx");
    }

    private void ResizeD3DTarget(int width, int height)
    {
        Debug.Assert(_image != null);
        Debug.Assert(_d3dDevice != 0);

        ReleaseCom(ref _d3dSurface);
        ReleaseCom(ref _d3dTexture);

        var createTexture =
            GetComMethod<CreateTextureDelegate>(_d3dDevice, 23);

        int hr = createTexture(
            _d3dDevice,
            (uint)width,
            (uint)height,
            1,
            D3DUsageRenderTarget,
            D3DFormatA8R8G8B8,
            D3DPoolDefault,
            out _d3dTexture,
            null);

        ThrowIfFailed(hr, "IDirect3DDevice9::CreateTexture");

        var getSurfaceLevel =
            GetComMethod<GetSurfaceLevelDelegate>(_d3dTexture, 18);

        hr = getSurfaceLevel(
            _d3dTexture,
            0,
            out _d3dSurface);

        ThrowIfFailed(hr, "IDirect3DTexture9::GetSurfaceLevel");

        InvokeImage(() =>
        {
            _image.Lock();

            try
            {
                _image.SetBackBuffer(
                    D3DResourceType.IDirect3DSurface9,
                    _d3dSurface,
                    enableSoftwareFallback: false);
            }
            finally
            {
                _image.Unlock();
            }
        });
    }

    private void InvalidateImage()
    {
        Debug.Assert(_image != null);

        InvokeImage(() =>
        {
            if (_d3dSurface == 0 || !_image.IsFrontBufferAvailable)
                return;

            _image.Lock();

            try
            {
                _image.AddDirtyRect(
                    new Int32Rect(
                        0,
                        0,
                        _targetWidth,
                        _targetHeight));
            }
            finally
            {
                _image.Unlock();
            }
        });
    }

    private void DetachBackBuffer()
    {
        if (_image == null)
            return;

        _image.Lock();

        try
        {
            _image.SetBackBuffer(
                D3DResourceType.IDirect3DSurface9,
                nint.Zero);
        }
        finally
        {
            _image.Unlock();
        }
    }

    private void InvokeImage(Action action)
    {
        Debug.Assert(_image != null);

        if (_image.Dispatcher.CheckAccess())
            action();
        else
            _image.Dispatcher.Invoke(action);
    }

    private void RecreateTarget()
    {
        Debug.Assert(_gl != null);
        Debug.Assert(_d3dDevice != 0);

        var width = Math.Max(1, (int)Math.Ceiling(Size.X));
        var height = Math.Max(1, (int)Math.Ceiling(Size.Y));

        if (width == _targetWidth && height == _targetHeight)
            return;

        if (_render == null)
            return;

        DestroyInterop();

        ResizeD3DTarget(width, height);

        if (_renderTarget == null)
        {
            _colorTex = new GlTexture(_gl)
            {
                SampleCount = _render.Options.SampleCount,
                MinFilter = TextureMinFilter.Linear,
                MagFilter = TextureMagFilter.Linear,
                Target = TextureTarget.Texture2DMultisample,
                IsMutable = false
            };

            _interopTex = new GlTexture(_gl)
            {
                MinFilter = TextureMinFilter.Linear,
                MagFilter = TextureMagFilter.Linear
            };

            _colorTex.Update();
            _interopTex.Update();

            _depthBuffer = new GlRenderBuffer(_gl);
            _renderTarget = new GlTextureRenderTarget(_gl);
            _renderTarget.Flags = GlRenderTargetFlags.Main;

            _interopFrameBuffer = new GlTextureFrameBuffer(_gl);
        }

        _interopTex!.OverrideSize((uint)width, (uint)height);

        _colorTex!.Recreate();
        _colorTex.Allocate(
            (uint)width,
            (uint)height,
            1,
            TextureFormat.Rgba32);

        _depthBuffer!.Update(
            (uint)width,
            (uint)height,
            _colorTex.SampleCount,
            InternalFormat.Depth24Stencil8);

        _renderTarget.FrameBuffer.Configure(
            _colorTex,
            _depthBuffer,
            _colorTex.SampleCount);

        _interopObject = _dxInterop!.DxregisterObject(
            _interopDevice,
            (void*)_d3dTexture,
            _interopTex.Handle,
            (NV)GLEnum.Texture2D,
            NV.AccessWriteDiscardNV);

        if (_interopObject == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastSystemError(),
                "wglDXRegisterObjectNV failed.");
        }

        _interopFrameBuffer!.Configure(_interopTex, null, 1);

        _targetWidth = width;
        _targetHeight = height;
    }

    private void LockTarget()
    {
        nint obj = _interopObject;

        if (!_dxInterop!.DxlockObjects(
                _interopDevice,
                1,
                &obj))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "wglDXLockObjectsNV failed.");
        }
    }

    private void UnlockTarget()
    {
        nint obj = _interopObject;

        if (!_dxInterop!.DxunlockObjects(
                _interopDevice,
                1,
                &obj))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "wglDXUnlockObjectsNV failed.");
        }
    }

    private void OnCompositionRendering(object? sender, EventArgs args)
    {
        if (!_vsyncEnabled)
            return;

        _compositionFrameIndex++;

        if (_compositionFrameIndex >= _vsyncScale)
        {
            _compositionFrameIndex = 0;
            _compositionFrame.Set();
        }
    }

    private void QueueInvalidate()
    {
        if (_image == null ||
            Interlocked.Exchange(ref _invalidatePending, 1) != 0)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                if (_image != null)
                    InvalidateImage();
            }
            finally
            {
                Volatile.Write(ref _invalidatePending, 0);
            }
        });
    }

    private void DestroyInterop()
    {
        if (_gl == null)
            return;

        if (_interopObject != 0)
        {
            _dxInterop!.DxunregisterObject(
                _interopDevice,
                _interopObject);

            _interopObject = 0;
        }
    }

    private void DisposePresentationTarget()
    {
        if (_glCtx == 0)
            return;

        TakeContext();

        DestroyInterop();

        _interopTex?.Dispose();
        _interopFrameBuffer?.Dispose();
        _renderTarget?.Dispose();
        _colorTex?.Dispose();
        _depthBuffer?.Dispose();

        _renderTarget = null;
        _colorTex = null;
        _depthBuffer = null;
        _interopFrameBuffer = null;
        _interopTex = null;

        if (_interopDevice != 0)
        {
            _dxInterop!.DxcloseDevice(_interopDevice);
            _interopDevice = 0;
        }

        if (_image != null)
            InvokeImage(DetachBackBuffer);

        ReleaseCom(ref _d3dSurface);
        ReleaseCom(ref _d3dTexture);
        ReleaseCom(ref _d3dDevice);
        ReleaseCom(ref _d3d);

        Source = null;
        _image = null;

        _mainCtx?.Release();
        _mainCtx = null;

        _wgl.DeleteContext(_glCtx);
        _glCtx = 0;

        if (_hdc != 0 && _hiddenWnd != 0)
        {
            ReleaseDC(_hiddenWnd, _hdc);
            _hdc = 0;
        }

        if (_hiddenWnd != 0)
        {
            DestroyWindow(_hiddenWnd);
            _hiddenWnd = 0;
        }
    }

    public void Dispose()
    {
        DisposePresentationTarget();
        GC.SuppressFinalize(this);
    }

    public override IRenderEngine CreateRenderEngine(
        object? driverOptions)
    {
        var glOptions =
            driverOptions as GlRenderOptions ??
            new GlRenderOptions();

        glOptions.FloatPrecision = ShaderPrecision.High;
        glOptions.Outline.Use = true;

        _render = new OpenGLRender(_gl!, glOptions);

        TakeContext();

#if DEBUG
        if (EditorDebug.DebugEnabled)
            _render.EnableDebug(EditorDebug.DebugSync);
#endif

        return _render;
    }

    public override void EnableVSync(bool enable, int scale = 1)
    {
        Debug.Assert(scale > 0);

        _vsyncScale = scale;
        _compositionFrameIndex = 0;
        _vsyncEnabled = enable;

        if (!enable)
            _compositionFrame.Set();
    }

    public override void SwapBuffers()
    {

        if (_vsyncEnabled)
            _compositionFrame.WaitOne();
    }

    public override void BeginFrame(long frameNum)
    {
        RecreateTarget();

        _render!.SetRenderTarget(_renderTarget);
        _render.PushGroup($"Begin frame {frameNum}");
    }

    public override void EndFrame()
    {
        LockTarget();

        _gl!.Flush();

        _renderTarget!.FrameBuffer.CopyTo(
            _interopFrameBuffer!);

        UnlockTarget();

        _render!.PopGroup();

        QueueInvalidate();
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

    public IXrGraphicDriver CreateXrDriver()
    {
        return new XrOpenGLGraphicDriver(this);
    }

    public bool TryGetProcAddress(
        string proc,
        out nint addr,
        int? slot = null)
    {
        addr = GetProcAddress(proc);
        return addr != 0;
    }

    public nint GetProcAddress(
        string proc,
        int? slot = null)
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

        return WinGlContext.Attach(
            _hdc,
            newCtx,
            _gl,
            _wgl);
    }

    private void EnsureWglExtensionsLoaded()
    {
        if (_wglExtensionsLoaded)
            return;

        lock (WglBootstrapLock)
        {
            if (_wglExtensionsLoaded)
                return;

            nint window = 0;
            nint hdc = 0;
            nint context = 0;

            try
            {
                window = CreateHiddenWindow();
                hdc = GetDC(window);

                if (hdc == 0)
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());

                var pfd = CreatePixelFormatDescriptor();
                var pixelFormat = _wgl.ChoosePixelFormat(hdc, ref pfd);

                if (pixelFormat <= 0)
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());

                if (!_wgl.SetPixelFormat(hdc, pixelFormat, ref pfd))
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());

                context = _wgl.CreateContext(hdc);

                if (context == 0)
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());

                if (!_wgl.MakeCurrent(hdc, context))
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());

                LoadWglExtensions();

                _wglExtensionsLoaded = true;
            }
            finally
            {
                _wgl.MakeCurrent(0, 0);

                if (context != 0)
                    _wgl.DeleteContext(context);

                if (hdc != 0 && window != 0)
                    ReleaseDC(window, hdc);

                if (window != 0)
                    DestroyWindow(window);
            }
        }
    }

    private void LoadWglExtensions()
    {
        ChoosePixelFormatARB =
            Marshal.GetDelegateForFunctionPointer<
                wglChoosePixelFormatARBPtr>(
                _wgl.GetProcAddress(
                    "wglChoosePixelFormatARB"));

        CreateContextAttribsARB =
            Marshal.GetDelegateForFunctionPointer<
                wglCreateContextAttribsARBPtr>(
                _wgl.GetProcAddress(
                    "wglCreateContextAttribsARB"));

        GetPixelFormatAttribivARB =
            Marshal.GetDelegateForFunctionPointer<
                wglGetPixelFormatAttribivARBPtr>(
                _wgl.GetProcAddress(
                    "wglGetPixelFormatAttribivARB"));
    }

    private static PixelFormatDescriptor
        CreatePixelFormatDescriptor()
    {
        return new PixelFormatDescriptor
        {
            NSize =
                (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),

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

    private static nint CreateHiddenWindow()
    {
        var hInstance = GetModuleHandle(null);
        const string className = "XrEngineHiddenWglWindow";

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
            int error = Marshal.GetLastWin32Error();

            if (error != ERROR_CLASS_ALREADY_EXISTS)
                throw new Win32Exception(error);
        }

        nint window = CreateWindowExW(
            0,
            className,
            "Hidden WGL",
            WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            1,
            1,
            0,
            0,
            hInstance,
            0);

        if (window == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return window;
    }

    private static T GetComMethod<T>(
        nint instance,
        int slot)
        where T : Delegate
    {
        nint vtable = Marshal.ReadIntPtr(instance);
        nint address = Marshal.ReadIntPtr(
            vtable,
            slot * nint.Size);

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static void ReleaseCom(ref nint instance)
    {
        if (instance == 0)
            return;

        GetComMethod<ReleaseDelegate>(instance, 2)(instance);
        instance = 0;
    }

    private static void ThrowIfFailed(
        int hr,
        string operation)
    {
        if (hr < 0)
            throw new COMException($"{operation} failed.", hr);
    }

    IGlContext? IGlContextProvider.Current =>
        WinGlContext.Current;

    public GL Gl =>
        _gl ?? throw new NullReferenceException();

    public nint HDc => _hdc;
    public nint GlCtx => _glCtx;

    public override nint HWnd => _hiddenWnd;
    public override bool SupportsDualRender => true;
}
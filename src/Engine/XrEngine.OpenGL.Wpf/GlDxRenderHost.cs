using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D9;
using Silk.NET.WGL;
using Silk.NET.WGL.Extensions.NV;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using PresentParameters = Silk.NET.Direct3D9.PresentParameters;
using System.Windows.Media;
using XrEngine.Wpf;


#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

namespace XrEngine.OpenGL.Wpf;

public unsafe class GlDxRenderHost : ImageRenderHost, INativeContext,
    IGlContextProvider, IDisposable
{
    protected GL? _gl;
    protected readonly WGL _wgl;
    protected readonly bool _createContext;
    protected nint _glCtx;
    protected nint _hdc;
    protected bool _useEs;
    protected WinGlContext? _mainCtx;

    private readonly D3D9 _d3d9 = D3D9.GetApi(null);
    private nint _hiddenWnd;
    private D3DImage? _image;

    private ComPtr<IDirect3D9Ex> _d3d;
    private ComPtr<IDirect3DDevice9Ex> _d3dDevice;
    private ComPtr<IDirect3DTexture9> _d3dTexture;
    private ComPtr<IDirect3DSurface9> _d3dSurface;

    private NVDXInterop? _dxInterop;
    private nint _interopDevice;
    private nint _interopObject;

    private int _targetWidth;
    private int _targetHeight;
    private int _invalidatePending;

    private bool _vsyncEnabled;
    private int _vsyncScale = 1;
    private uint _refreshRate;
    private GlTexture? _colorTex;
    private GlTexture? _interopTex;
    private GlRenderBuffer? _depthBuffer;
    private GlTextureRenderTarget? _renderTarget;
    private GlTextureFrameBuffer? _interopFrameBuffer;
    private OpenGLRender? _render;
    private readonly HighResolutionTimer _timer;

    #region WGL interop

    protected static wglCreateContextAttribsARBPtr? CreateContextAttribsARB;
    protected static wglChoosePixelFormatARBPtr? ChoosePixelFormatARB;
    protected static wglSwapIntervalEXTPtr? SwapIntervalEXT;
    protected static wglGetPixelFormatAttribivARBPtr? GetPixelFormatAttribivARB;
    private long _lasftFrameTime;

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

    private const int ErrorClassAlreadyExists = 1410;
    private const int CsOwnDc = 0x0020;
    private const int CwUseDefault = unchecked((int)0x80000000);
    private const int WsOverlappedWindow = 0x00CF0000;

    private const byte PfdTypeRgba = 0;
    private const byte PfdMainPlane = 0;

    private const uint PfdDoubleBuffer = 0x00000001;
    private const uint PfdDrawToWindow = 0x00000004;
    private const uint PfdSupportOpenGl = 0x00000020;
    private const uint PfdDirect3DAccelerated = 0x00004000;
    private const uint PfdSupportComposition = 0x00008000;

    private const int WglDrawToWindowArb = 0x2001;
    private const int WglSupportOpenGlArb = 0x2010;
    private const int WglDoubleBufferArb = 0x2011;
    private const int WglPixelTypeArb = 0x2013;
    private const int WglTypeRgbaArb = 0x202B;
    private const int WglColorBitsArb = 0x2014;
    private const int WglAlphaBitsArb = 0x201B;
    private const int WglDepthBitsArb = 0x2022;
    private const int WglStencilBitsArb = 0x2023;
    private const int WglFramebufferSrgbCapableArb = 0x20A9;

    private const int WglContextMajorVersionArb = 0x2091;
    private const int WglContextMinorVersionArb = 0x2092;
    private const int WglContextProfileMaskArb = 0x9126;
    private const int WglContextCoreProfileBitArb = 0x00000001;
    private const int WglContextEsProfileBitExt = 0x00000004;

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

    private static readonly WndProc2 DummyWndProc = DefWindowProcW;

    #endregion

    public GlDxRenderHost(bool createContext = true, bool useEs = false)
    {
        Context.Implement<IGlContextProvider>(this);

        DefaultNativeContext.TryCreate("Opengl32.dll", out var opengl);
        DefaultNativeContext.TryCreate("Gdi32.dll", out var gdi);

        _wgl = new WGL(new MultiNativeContext(gdi, opengl));
        _createContext = createContext;
        _useEs = useEs;
        _timer = new HighResolutionTimer();

        VisualBitmapScalingMode = BitmapScalingMode.NearestNeighbor;

        ColorFormat = TextureFormat.Rgba32;
    }

    protected override void OnHostLoaded()
    {
        if (!_createContext || _glCtx != 0)
            return;

        EnsureWglExtensionsLoaded();

        _hiddenWnd = CreateHiddenWindow();

        CreateContext(_hiddenWnd);

#if GL_WRAPPER
        _gl = new OpenGLWrapper.GlSwitchWrapper(Silk.NET.OpenGL.GL.GetApi(this));
#else
        _gl = GL.GetApi(this);
#endif

        _mainCtx = WinGlContext.Attach(_hdc, _glCtx, _gl, _wgl);
        _mainCtx.Take();

        if (!WGL.GetApi().TryGetExtension(out _dxInterop))
            throw new NotSupportedException("WGL_NV_DX_interop is not available.");

        _image = new D3DImage();
        Source = _image;

        CreateD3DDevice();

        _interopDevice = _dxInterop!.DxopenDevice(_d3dDevice.Handle);

        if (_interopDevice == 0)
            throw new InvalidOperationException("wglDXOpenDeviceNV failed for the D3D9Ex device.");

        RecreateTarget();
    }

    protected override void OnHostUnloaded()
    {
        DisposePresentationTarget();
    }

    protected virtual void CreateContext(nint hWnd)
    {
        _hdc = GetDC(hWnd);

        if (_hdc == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var pfd = CreatePixelFormatDescriptor();

        int[] pfAttribs =
        [
            WglDrawToWindowArb, 1,
            WglSupportOpenGlArb, 1,
            WglDoubleBufferArb, 1,
            WglPixelTypeArb, WglTypeRgbaArb,
            WglColorBitsArb, 24,
            WglAlphaBitsArb, 8,
            WglDepthBitsArb, 24,
            WglStencilBitsArb, 8,
            WglFramebufferSrgbCapableArb, 1,
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
            throw new NotSupportedException("No sRGB-capable WGL pixel format found.");
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
            WglContextMajorVersionArb, 4,
            WglContextMinorVersionArb, 6,
            WglContextProfileMaskArb,
            WglContextCoreProfileBitArb,
            0
        ]
        :
        [
            WglContextMajorVersionArb, 3,
            WglContextMinorVersionArb, 2,
            WglContextProfileMaskArb,
            WglContextEsProfileBitExt,
            0
        ];

        var result = CreateContextAttribsARB!(_hdc, 0, attributes);

        if (result == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return result;
    }

    private void CreateD3DDevice()
    {
        SilkMarshal.ThrowHResult(_d3d9.Direct3DCreate9Ex(D3D9.SdkVersion, ref _d3d));

        var parameters = new PresentParameters
        {
            BackBufferWidth = 1,
            BackBufferHeight = 1,
            BackBufferFormat = Silk.NET.Direct3D9.Format.Unknown,
            BackBufferCount = 1,
            SwapEffect = Swapeffect.Discard,
            HDeviceWindow = _hiddenWnd,
            Windowed = true,
            PresentationInterval = D3D9.PresentIntervalImmediate
        };

        uint flags =
            D3D9.CreateFpuPreserve |
            D3D9.CreateMultithreaded |
            D3D9.CreateHardwareVertexprocessing;

        SilkMarshal.ThrowHResult(
            _d3d.CreateDeviceEx(
                D3D9.AdapterDefault,
                Devtype.Hal,
                _hiddenWnd,
                flags,
                ref parameters,
                null,
                ref _d3dDevice));
    }

    private void ResizeD3DTarget(int width, int height)
    {
        Debug.Assert(_image != null);
        Debug.Assert(_d3dDevice.Handle != null);

        _d3dSurface.Dispose();
        _d3dSurface = default;

        _d3dTexture.Dispose();
        _d3dTexture = default;

        SilkMarshal.ThrowHResult(
            _d3dDevice.Handle->CreateTexture(
                (uint)width,
                (uint)height,
                1,
                D3D9.UsageRendertarget,
                Silk.NET.Direct3D9.Format.A8R8G8B8,
                Pool.Default,
                _d3dTexture.GetAddressOf(),
                null));

        SilkMarshal.ThrowHResult(_d3dTexture.Handle->GetSurfaceLevel(0, _d3dSurface.GetAddressOf()));

        InvokeImage(() =>
        {
            _image.Lock();

            try
            {
                _image.SetBackBuffer(
                    D3DResourceType.IDirect3DSurface9,
                    (nint)_d3dSurface.Handle,
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
            if (_d3dSurface.Handle == null || !_image.IsFrontBufferAvailable)
                return;

            _image.Lock();

            try
            {
                _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
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
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, nint.Zero);
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

    private void CreateGlObjects()
    {
        Debug.Assert(_gl != null && _render != null);

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

        _colorTex.UpdateSampler();
        _interopTex.UpdateSampler();

        _depthBuffer = new GlRenderBuffer(_gl);

        _renderTarget = new GlTextureRenderTarget(_gl)
        {
            Flags = GlRenderTargetFlags.Main | GlRenderTargetFlags.ForceSrgbEncode
        };

        _interopFrameBuffer = new GlTextureFrameBuffer(_gl);
    }

    private void RecreateTarget()
    {
        if (_render == null)
            return;

        Debug.Assert(_gl != null);
        Debug.Assert(_d3dDevice.Handle != null);

        var width = Math.Max(1, (int)Math.Ceiling(Size.X));
        var height = Math.Max(1, (int)Math.Ceiling(Size.Y));

        if (width == _targetWidth && height == _targetHeight)
            return;

        DisposeInterop();

        ResizeD3DTarget(width, height);

        if (_renderTarget == null)
            CreateGlObjects();

        Debug.Assert(_interopTex != null && _colorTex != null && _renderTarget != null && _depthBuffer != null);

        _interopTex.OverrideSize((uint)width, (uint)height);

        _colorTex.Recreate();
        _colorTex.Allocate((uint)width, (uint)height, 1, ColorFormat);

        _depthBuffer.Update((uint)width, (uint)height, _colorTex.SampleCount, InternalFormat.Depth24Stencil8);

        _renderTarget.FrameBuffer.Configure(_colorTex, _depthBuffer, _colorTex.SampleCount);

        _interopObject = _dxInterop!.DxregisterObject(
            _interopDevice,
            _d3dTexture.Handle,
            _interopTex.Handle,
            (NV)GLEnum.Texture2D,
            NV.AccessWriteDiscardNV);

        if (_interopObject == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "wglDXRegisterObjectNV failed.");

        _interopFrameBuffer!.Configure(_interopTex, null, 1);

        _targetWidth = width;
        _targetHeight = height;
    }

    private void LockTarget()
    {
        var obj = _interopObject;

        if (!_dxInterop!.DxlockObjects(_interopDevice, 1, &obj))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "wglDXLockObjectsNV failed.");
    }

    private void UnlockTarget()
    {
        var obj = _interopObject;

        if (!_dxInterop!.DxunlockObjects(_interopDevice, 1, &obj))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "wglDXUnlockObjectsNV failed.");
    }

    private void QueueInvalidate()
    {
        if (_image == null || Interlocked.Exchange(ref _invalidatePending, 1) != 0)
            return;

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

    private void DisposeInterop()
    {
        if (_interopObject != 0)
        {
            _dxInterop!.DxunregisterObject(_interopDevice, _interopObject);
            _interopObject = 0;
        }
    }

    private void DisposePresentationTarget()
    {
        if (_glCtx == 0)
            return;

        TakeContext();

        DisposeInterop();

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

        _d3dSurface.Dispose();
        _d3dSurface = default;

        _d3dTexture.Dispose();
        _d3dTexture = default;

        _d3dDevice.Dispose();
        _d3dDevice = default;

        _d3d.Dispose();
        _d3d = default;

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

        _d3d9.Dispose();

        _timer.Dispose();

        GC.SuppressFinalize(this);
    }

    public override IRenderEngine CreateRenderEngine(object? driverOptions)
    {
        TakeContext();

        var glOptions =
            driverOptions as GlRenderOptions ??
            new GlRenderOptions();

        glOptions.FloatPrecision = ShaderPrecision.High;
        glOptions.Outline.Use = true;

        _render = new OpenGLRender(_gl!, glOptions);

        if (glOptions.UseResolve)
            ColorFormat = TextureFormat.RgbaFloat16;

        return _render;
    }

    public override void EnableVSync(bool enable, int scale = 1)
    {
        Debug.Assert(scale > 0);

        _vsyncEnabled = enable;
        _vsyncScale = scale;
        _refreshRate = DisplayUtils.GetRefreshRate(_hiddenWnd);
    }

    protected void WaitNextFrame()
    {
        var frameTime = (long)(Stopwatch.Frequency / (_refreshRate / (float)_vsyncScale));

        var curTime = Stopwatch.GetTimestamp();

        var remaining = frameTime - (curTime - _lasftFrameTime);

        if (remaining > 0)
            _timer.Sleep(TimeSpan.FromTicks(remaining));

        _lasftFrameTime = Stopwatch.GetTimestamp();
    }

    public override void SwapBuffers()
    {
        Debug.Assert(_gl != null && _renderTarget != null && _interopFrameBuffer != null);

        _gl.Finish();

        LockTarget();

        _renderTarget.FrameBuffer.CopyTo(_interopFrameBuffer);

        UnlockTarget();

        QueueInvalidate();

        if (_vsyncEnabled)
            WaitNextFrame();
    }

    public override void BeginFrame(long frameNum)
    {
        RecreateTarget();

        _render!.SetRenderTarget(_renderTarget);
        _render.PushGroup($"Begin frame {frameNum}");
    }

    public override void EndFrame()
    {
        _render!.PopGroup();
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

    private void EnsureWglExtensionsLoaded()
    {
        if (CreateContextAttribsARB != null)
            return;

        nint window = 0;
        nint hdc = 0;
        nint context = 0;

        try
        {
            window = CreateHiddenWindow();
            hdc = GetDC(window);

            if (hdc == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            var pfd = CreatePixelFormatDescriptor();
            var pixelFormat = _wgl.ChoosePixelFormat(hdc, ref pfd);

            if (pixelFormat <= 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!_wgl.SetPixelFormat(hdc, pixelFormat, ref pfd))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            context = _wgl.CreateContext(hdc);

            if (context == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!_wgl.MakeCurrent(hdc, context))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            LoadWglExtensions();
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

    private void LoadWglExtensions()
    {
        ChoosePixelFormatARB = Marshal.GetDelegateForFunctionPointer<
                wglChoosePixelFormatARBPtr>(_wgl.GetProcAddress("wglChoosePixelFormatARB"));

        CreateContextAttribsARB = Marshal.GetDelegateForFunctionPointer<
                wglCreateContextAttribsARBPtr>(_wgl.GetProcAddress("wglCreateContextAttribsARB"));

        GetPixelFormatAttribivARB = Marshal.GetDelegateForFunctionPointer<
                wglGetPixelFormatAttribivARBPtr>(_wgl.GetProcAddress("wglGetPixelFormatAttribivARB"));
    }

    private static PixelFormatDescriptor CreatePixelFormatDescriptor()
    {
        return new PixelFormatDescriptor
        {
            NSize = (ushort)Marshal.SizeOf<PixelFormatDescriptor>(),

            NVersion = 1,
            IPixelType = PfdTypeRgba,

            DwFlags =
                PfdSupportOpenGl |
                PfdSupportComposition |
                PfdDirect3DAccelerated |
                PfdDrawToWindow |
                PfdDoubleBuffer,

            ILayerType = PfdMainPlane,
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
            style = CsOwnDc,
            lpfnWndProc = DummyWndProc,
            hInstance = hInstance,
            lpszClassName = className
        };

        var atom = RegisterClassW(ref wc);

        if (atom == 0)
        {
            var error = Marshal.GetLastWin32Error();

            if (error != ErrorClassAlreadyExists)
                throw new Win32Exception(error);
        }

        var window = CreateWindowExW(
            0,
            className,
            "Hidden WGL",
            WsOverlappedWindow,
            CwUseDefault,
            CwUseDefault,
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

    IGlContext? IGlContextProvider.Current => WinGlContext.Current;

    public GL Gl => _gl ?? throw new NullReferenceException();

    public nint HDc => _hdc;

    public nint GlCtx => _glCtx;

    public override nint HWnd => _hiddenWnd;

    public override bool SupportsDualRender => true;

    public TextureFormat ColorFormat { get; set; }
}

using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using CefSharp.Structs;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.OpenGL;
using Silk.NET.WGL;
using Silk.NET.WGL.Extensions.NV;
using System.Runtime.InteropServices;

namespace XrEngine.Browser.Win
{
    public enum BrowserEye
    {
        Mono = 0,
        Left = 1,
        Right = 2,
        None = 3
    }

    public sealed class GlRenderHandler : IRenderHandler, IDisposable
    {
        private class InteropTarget
        {
            public ComPtr<ID3D11Texture2D> Texture;

            public ComPtr<ID3D11Resource> Resource;

            public uint GlTexture;

            public nint InteropObject;

            public TaskCompletionSource<long>? PaintTask;

            public bool HasValue;
        }

        private struct DummySource : INativeWindowSource
        {
            public INativeWindow? Native => null;
        }

        private IntPtr _currentCursor;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        private readonly GL _gl;
        private readonly D3D11 _d3d11;
        private readonly NVDXInterop _dxInterop;

        private readonly int _width;
        private readonly int _height;
        private readonly bool _isStereo;

        private ComPtr<ID3D11Device> _device;
        private ComPtr<ID3D11DeviceContext> _context;
        private ComPtr<ID3D11Device1> _device1;

        private readonly nint _interopDevice;

        private InteropTarget? _mono;
        private InteropTarget? _left;
        private InteropTarget? _right;

        private BrowserEye _captureEye;
        private bool _disposed;
        private int _frameRequested;
        private bool _frameReady;

        private readonly object _lock = new();

        public unsafe GlRenderHandler(GL gl, int width, int height, bool isStereo = false)
        {
            _gl = gl;
            _width = width;
            _height = height;
            _isStereo = isStereo;
            _d3d11 = D3D11.GetApi(new DummySource());

            if (!WGL.GetApi().TryGetExtension(out _dxInterop))
                throw new NotSupportedException();

            SilkMarshal.ThrowHResult(
                _d3d11.CreateDevice(
                    default(ComPtr<IDXGIAdapter>),
                    D3DDriverType.Hardware,
                    Software: default,
                    (uint)(CreateDeviceFlag.BgraSupport | CreateDeviceFlag.Debug),
                    null,
                    0,
                    D3D11.SdkVersion,
                    ref _device,
                    null,
                    ref _context));

            _device1 = _device.QueryInterface<ID3D11Device1>();

            _interopDevice = _dxInterop.DxopenDevice(_device.Handle);

            if (_interopDevice == 0)
                throw new InvalidOperationException("NVDXInterop.DxopenDevice failed.");

            _left = CreateInteropTarget();
            _right = CreateInteropTarget();
            _mono = CreateInteropTarget();

            _captureEye = _isStereo ? BrowserEye.None : BrowserEye.Mono;
        }

        private unsafe InteropTarget CreateInteropTarget()
        {
            var target = new InteropTarget();

            target.Texture = CreateD3DTexture();
            target.Resource = target.Texture.QueryInterface<ID3D11Resource>();

            target.GlTexture = _gl.GenTexture();

            _gl.BindTexture(TextureTarget.Texture2D, target.GlTexture);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.BindTexture(TextureTarget.Texture2D, 0);

            target.InteropObject = _dxInterop.DxregisterObject(
                _interopDevice,
                target.Texture.Handle,
                target.GlTexture,
                (NV)GLEnum.Texture2D,
                NV.AccessReadOnlyNV);

            if (target.InteropObject == 0)
                throw new InvalidOperationException("NVDXInterop.DxregisterObject failed.");

            target.HasValue = true;

            return target;
        }

        private unsafe ComPtr<ID3D11Texture2D> CreateD3DTexture()
        {
            var desc = new Texture2DDesc
            {
                Width = (uint)_width,
                Height = (uint)_height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.FormatB8G8R8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                Usage = Usage.Default,
                BindFlags = (uint)(BindFlag.ShaderResource | BindFlag.RenderTarget),
                CPUAccessFlags = 0,
                MiscFlags = (uint)ResourceMiscFlag.Shared
            };

            ComPtr<ID3D11Texture2D> texture = default;

            SilkMarshal.ThrowHResult(
                _device.CreateTexture2D(
                    in desc,
                    null,
                    ref texture));

            return texture;
        }

        public void ClearFrameRequest()
        {
            lock (_lock)
            {
                _frameRequested = 0;
                _captureEye = BrowserEye.None;
            }
        }

        public void CaptureNextFrame(BrowserEye eye)
        {
            lock (_lock)
            {
                _captureEye = eye;

                if (eye == BrowserEye.None)
                    _frameRequested--;
            }
        }

        public Task<long> WaitNextPaintAsync(BrowserEye eye)
        {
            var target = GetTarget(eye);

            lock (_lock)
            {
                target!.PaintTask = new TaskCompletionSource<long>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

                return target.PaintTask.Task;
            }
        }

        public void OnAcceleratedPaint(
            PaintElementType type,
            Rect dirtyRect,
            AcceleratedPaintInfo acceleratedPaintInfo)
        {
            var handle = acceleratedPaintInfo.SharedTextureHandle;

            if (handle == nint.Zero)
                return;

            var eye = _isStereo ? _captureEye : BrowserEye.Mono;

            if (eye == BrowserEye.None)
            {
                lock (_lock)
                    _frameRequested = 2;

                eye = BrowserEye.Mono;
            }

            var target = GetTarget(eye);

            TaskCompletionSource<long>? paintTask = null;

            long frameVersion = 0;

            Log.Debug(this, "Render on {0}", eye);

            lock (_lock)
            {
                using var cefTexture = OpenCefTexture(handle);
                using var cefResource = cefTexture.QueryInterface<ID3D11Resource>();

                try
                {
                    _context.CopyResource(target!.Resource, cefResource);
                    _context.Flush();

                    paintTask = target.PaintTask;
                    target.PaintTask = null;

                    _frameReady = true;
                }
                catch
                {
                    return;
                }
            }

            paintTask?.TrySetResult(frameVersion);
        }

        private unsafe ComPtr<ID3D11Texture2D> OpenCefTexture(nint handle)
        {
            if (handle == nint.Zero)
                throw new InvalidOperationException("CEF shared texture handle is zero.");

            var iid = ID3D11Texture2D.Guid;

            void* result = null;

            var hr = _device1.OpenSharedResource1(
                (void*)handle,
                &iid,
                &result);

            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            return new ComPtr<ID3D11Texture2D>((ID3D11Texture2D*)result);
        }

        public bool UpdateTexture(uint targetGlTexture)
        {
            return UpdateTexture(targetGlTexture, BrowserEye.Mono);
        }

        public unsafe bool UpdateTexture(uint targetGlTexture, BrowserEye eye)
        {
            var target = GetTarget(eye);

            lock (_lock)
            {
                var obj = target!.InteropObject;

                if (!_dxInterop.DxlockObjects(_interopDevice, 1, &obj))
                    return false;

                try
                {
                    _gl.CopyImageSubData(
                        target.GlTexture,
                        GLEnum.Texture2D,
                        0,
                        0,
                        0,
                        0,
                        targetGlTexture,
                        GLEnum.Texture2D,
                        0,
                        0,
                        0,
                        0,
                        (uint)_width,
                        (uint)_height,
                        1);

                    _frameReady = false;

                    return true;
                }
                finally
                {
                    _dxInterop.DxunlockObjects(_interopDevice, 1, &obj);

                    _context.Flush();
                }
            }
        }

        private InteropTarget? GetTarget(BrowserEye eye)
        {
            if (!_isStereo || eye == BrowserEye.Mono)
                return _mono;

            if (eye == BrowserEye.Left)
                return _left;

            if (eye == BrowserEye.Right)
                return _right;

            throw new ArgumentOutOfRangeException(nameof(eye));
        }

        public void OnPaint(
            PaintElementType type,
            Rect dirtyRect,
            IntPtr buffer,
            int width,
            int height)
        {
        }

        public void OnPopupShow(bool show)
        {
        }

        public void OnPopupSize(Rect rect)
        {
        }

        public void OnCursorChange(
            IntPtr cursor,
            CursorType type,
            CursorInfo customCursorInfo)
        {
            SetCursor(cursor);
            _currentCursor = cursor;
            return;
        }

        public bool StartDragging(
            IDragData dragData,
            DragOperationsMask mask,
            int x,
            int y)
        {
            return false;
        }

        public void UpdateDragCursor(DragOperationsMask operation)
        {
        }

        public void OnScrollOffsetChanged(double x, double y)
        {
        }

        public void OnImeCompositionRangeChanged(
            CefSharp.Structs.Range selectedRange,
            Rect[] characterBounds)
        {
        }

        public ScreenInfo? GetScreenInfo()
        {
            return new ScreenInfo
            {
                DeviceScaleFactor = 1.0f,
                Depth = 32,
                DepthPerComponent = 8,
                IsMonochrome = false,
                Rect = new Rect(0, 0, _width, _height),
                AvailableRect = new Rect(0, 0, _width, _height)
            };
        }

        public Rect GetViewRect()
        {
            return new Rect(0, 0, _width, _height);
        }

        public bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return true;
        }

        public void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            DisposeTarget(_mono);
            DisposeTarget(_left);
            DisposeTarget(_right);

            if (_interopDevice != 0)
                _dxInterop.DxcloseDevice(_interopDevice);

            _context.Dispose();
            _device.Dispose();
            _device1.Dispose();
        }

        private void DisposeTarget(InteropTarget? target)
        {
            if (target == null)
                return;

            if (target.InteropObject != 0)
            {
                _dxInterop.DxunregisterObject(_interopDevice, target.InteropObject);
                target.InteropObject = 0;
            }

            if (target.GlTexture != 0)
            {
                _gl.DeleteTexture(target.GlTexture);
                target.GlTexture = 0;
            }

            target.Resource.Dispose();
            target.Texture.Dispose();

            target.HasValue = false;
            target = null;
        }


        public int Width => _width;

        public int Height => _height;

        public bool IsStereo => _isStereo;

        public bool FrameReady
        {
            get
            {
                lock (_lock)
                    return _frameReady;
            }
        }

        public bool FrameRequested
        {
            get
            {
                lock (_lock)
                    return _frameRequested > 0;
            }
        }

    }
}
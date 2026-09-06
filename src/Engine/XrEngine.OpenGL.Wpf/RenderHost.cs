using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using XrInteraction;

namespace XrEngine.OpenGL.Wpf
{
    public abstract class RenderHost : HwndHost, IRenderSurface
    {
        private HwndSource? _hwndSource;

        #region NATIVE

        [DllImport("User32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("User32.dll")]
        static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(IntPtr hWnd);

        const ushort WM_MOUSEMOVE = 0x0200;

        const ushort WM_MBUTTONDOWN = 0x0207;
        const ushort WM_LBUTTONDOWN = 0x0201;
        const ushort WM_RBUTTONDOWN = 0x0204;

        const ushort WM_MBUTTONUP = 0x0208;
        const ushort WM_LBUTTONUP = 0x0202;
        const ushort WM_RBUTTONUP = 0x0205;
        const ushort WM_MOUSEWHEEL = 0x020A;

        const ushort MK_LBUTTON = 0x0001;
        const ushort MK_MBUTTON = 0x0010;
        const ushort MK_RBUTTON = 0x0002;

        const ushort WM_KEYDOWN = 0x0100;
        const ushort WM_KEYUP = 0x0101;

        const uint WS_CHILD = 0x40000000;

        #endregion

        public RenderHost()
        {
            Loaded += (_, _) => Ready?.Invoke(this, EventArgs.Empty);
            base.SizeChanged += (_, _) => SizeChanged?.Invoke(this, EventArgs.Empty);
            Focusable = true;
        }

        public void CapturePointer()
        {
            SetCapture(_hwndSource!.Handle);
        }

        public void ReleasePointer()
        {
            ReleaseCapture();
        }

        public IntPtr OnMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            Pointer2Event ev = new();

            switch (msg)
            {
                case WM_MOUSEMOVE:

                    ev.Position.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Position.Y = ((int)lParam) >> 16;

                    if (((uint)wParam & MK_LBUTTON) == MK_LBUTTON)
                        ev.Buttons |= Pointer2Button.Left;

                    if (((uint)wParam & MK_RBUTTON) == MK_RBUTTON)
                        ev.Buttons |= Pointer2Button.Right;

                    if (((uint)wParam & MK_MBUTTON) == MK_MBUTTON)
                        ev.Buttons |= Pointer2Button.Right;

                    PointerMove?.Invoke(ev);
                    break;
                case WM_MBUTTONDOWN:
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                    ev.Position.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Position.Y = ((int)lParam) >> 16;

                    if (msg == WM_MBUTTONDOWN)
                        ev.Buttons = Pointer2Button.Middle;
                    else if (msg == WM_LBUTTONDOWN)
                        ev.Buttons = Pointer2Button.Left;
                    else if (msg == WM_RBUTTONDOWN)
                        ev.Buttons = Pointer2Button.Right;

                    PointerDown?.Invoke(ev);

                    Keyboard.Focus(this);
                    SetFocus(hwnd);

                    break;
                case WM_MOUSEWHEEL:
                    ev.Position.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Position.Y = ((int)lParam) >> 16;
                    ev.WheelDelta = (int)wParam >> 16;

                    WheelMove?.Invoke(ev);

                    break;
                case WM_MBUTTONUP:
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                    ev.Position.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Position.Y = ((int)lParam) >> 16;

                    if (msg == WM_MBUTTONUP)
                        ev.Buttons = Pointer2Button.Middle;
                    else if (msg == WM_LBUTTONUP)
                        ev.Buttons = Pointer2Button.Left;
                    else if (msg == WM_RBUTTONUP)
                        ev.Buttons = Pointer2Button.Right;

                    PointerUp?.Invoke(ev);
                    break;

                case WM_KEYDOWN:
                    var key1 = (ushort)wParam;
                    KeyDown?.Invoke(new KeyboardEvent() { Key = (KeyCode)KeyInterop.KeyFromVirtualKey((int)wParam) });
                    break;
                case WM_KEYUP:
                    var key2 = (ushort)wParam;
                    KeyUp?.Invoke(new KeyboardEvent() { Key = (KeyCode)KeyInterop.KeyFromVirtualKey((int)wParam) });
                    break;
            }

            return IntPtr.Zero;
        }

        void IRenderSurface.Focus()
        {
            Keyboard.Focus(this);
            if (_hwndSource != null)
                SetFocus(_hwndSource.Handle);
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return new HandleRef(null, 0);

            _hwndSource = new HwndSource(0, (int)WS_CHILD, 0, 0, 0, "RenderView", hwndParent.Handle);
            _hwndSource.AddHook(OnMessage);

            return _hwndSource.CreateHandleRef();
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            if (_hwndSource != null)
            {
                _hwndSource.Dispose();
                _hwndSource = null;
            }
        }

        public virtual void EnableVSync(bool enable, int scale = 1)
        {
        }

        public virtual void SwapBuffers()
        {
        }
        public virtual void ReleaseContext()
        {
        }

        public virtual bool TakeContext()
        {
            return true;
        }

        public abstract IRenderEngine CreateRenderEngine(object? driverOptions);

        public virtual void BeginFrame(long frameNum)
        {

        }

        public virtual void EndFrame()
        {

        }

        public Vector2 Size
        {
            get
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                return new Vector2(
                    (float)(ActualWidth * dpi.DpiScaleX),
                    (float)(ActualHeight * dpi.DpiScaleY)
                );
            }
        }

        public new event EventHandler? SizeChanged;

        public event EventHandler? Ready;

        public event PointerEventDelegate? PointerDown;

        public event PointerEventDelegate? PointerUp;

        public event PointerEventDelegate? PointerMove;

        public event PointerEventDelegate? WheelMove;

        public new event KeyboardEventDelegate? KeyUp;

        public new event KeyboardEventDelegate? KeyDown;

        public IntPtr HWnd => _hwndSource!.Handle;

        public abstract bool SupportsDualRender { get; }
    }
}

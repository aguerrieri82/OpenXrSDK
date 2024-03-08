#if GLES
#else
#endif

using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using XrEngine;



namespace XrEditor
{
    public abstract class RenderHost : HwndHost, IRenderSurface
    {
        private HwndSource? _hwndSource;


        #region NATIVE

        [DllImport("User32.dll")]
        static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("User32.dll")]
        static extern bool ReleaseCapture();


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



        const uint WS_CHILD = 0x40000000;

        #endregion

        public RenderHost()
        {

            Loaded += (_, _) => Ready?.Invoke(this, EventArgs.Empty);
            base.SizeChanged += (_, _) => SizeChanged?.Invoke(this, EventArgs.Empty);
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
            PointerEvent ev = new PointerEvent();

            switch (msg)
            {
                case WM_MOUSEMOVE:

                    ev.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Y = ((int)lParam) >> 16;

                    if (((uint)wParam & MK_LBUTTON) == MK_LBUTTON)
                        ev.Buttons |= MouseButton.Left;

                    if (((uint)wParam & MK_RBUTTON) == MK_RBUTTON)
                        ev.Buttons |= MouseButton.Right;

                    if (((uint)wParam & MK_MBUTTON) == MK_MBUTTON)
                        ev.Buttons |= MouseButton.Right;

                    PointerMove?.Invoke(ev);
                    break;
                case WM_MBUTTONDOWN:
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                    ev.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Y = ((int)lParam) >> 16;

                    if (msg == WM_MBUTTONDOWN)
                        ev.Buttons = MouseButton.Middle;
                    else if (msg == WM_LBUTTONDOWN)
                        ev.Buttons = MouseButton.Left;
                    else if (msg == WM_RBUTTONDOWN)
                        ev.Buttons = MouseButton.Right;

                    PointerDown?.Invoke(ev);

                    break;
                case WM_MOUSEWHEEL:
                    ev.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Y = ((int)lParam) >> 16;
                    ev.WheelDelta = (int)wParam >> 16;

                    WheelMove?.Invoke(ev);

                    break;
                case WM_MBUTTONUP:
                case WM_LBUTTONUP:
                case WM_RBUTTONUP:
                    ev.X = (short)(((int)lParam) & 0x0000FFFF);
                    ev.Y = ((int)lParam) >> 16;

                    if (msg == WM_MBUTTONUP)
                        ev.Buttons = MouseButton.Middle;
                    else if (msg == WM_LBUTTONUP)
                        ev.Buttons = MouseButton.Left;
                    else if (msg == WM_RBUTTONUP)
                        ev.Buttons = MouseButton.Right;

                    PointerUp?.Invoke(ev);
                    break;
            }

            return IntPtr.Zero;
        }

        protected unsafe override HandleRef BuildWindowCore(HandleRef hwndParent)
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




        public virtual void EnableVSync(bool enable)
        {


        }

        public virtual void SwapBuffers()
        {

        }



        public virtual void ReleaseContext()
        {

        }

        public virtual void TakeContext()
        {

        }

        public abstract IRenderEngine CreateRenderEngine();

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

        public IntPtr HWnd => _hwndSource!.Handle;
    }
}

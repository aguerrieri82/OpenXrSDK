using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XrEngine.Wpf;
using XrInteraction;

namespace XrEngine.OpenGL.Wpf;

public abstract class ImageRenderHost : Image, IRenderSurface
{
    protected ImageRenderHost()
    {
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = new ScaleTransform(1.0, -1.0);

        Stretch = Stretch.Fill;
        Focusable = true;
        ClipToBounds = true;

        Loaded += (_, _) =>
        {
            OnHostLoaded();
            Ready?.Invoke(this, EventArgs.Empty);
        };

        Unloaded += (_, _) => OnHostUnloaded();
        base.SizeChanged += (_, _) => SizeChanged?.Invoke(this, EventArgs.Empty);

        MouseMove += OnMouseMove;
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        MouseWheel += OnMouseWheel;
    }

    protected virtual void OnHostLoaded()
    {
    }

    protected virtual void OnHostUnloaded()
    {
    }

    public void CapturePointer()
    {
        CaptureMouse();
    }

    public void ReleasePointer()
    {
        ReleaseMouseCapture();
    }

    private Pointer2Event CreatePointerEvent(MouseEventArgs args)
    {
        var point = args.GetPosition(this);
        var dpi = VisualTreeHelper.GetDpi(this);

        var result = new Pointer2Event();
        result.Position.X = (float)(point.X * dpi.DpiScaleX);
        result.Position.Y = (float)((ActualHeight - point.Y) * dpi.DpiScaleY);

        if (args.LeftButton == MouseButtonState.Pressed)
            result.Buttons |= Pointer2Button.Left;

        if (args.RightButton == MouseButtonState.Pressed)
            result.Buttons |= Pointer2Button.Right;

        if (args.MiddleButton == MouseButtonState.Pressed)
            result.Buttons |= Pointer2Button.Middle;

        return result;
    }

    private void OnMouseMove(object sender, MouseEventArgs args)
    {
        PointerMove?.Invoke(CreatePointerEvent(args));
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        Focus();

        var result = CreatePointerEvent(args);
        result.Buttons = args.ChangedButton switch
        {
            MouseButton.Left => Pointer2Button.Left,
            MouseButton.Right => Pointer2Button.Right,
            MouseButton.Middle => Pointer2Button.Middle,
            _ => result.Buttons
        };

        PointerDown?.Invoke(result);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs args)
    {
        var result = CreatePointerEvent(args);
        result.Buttons = args.ChangedButton switch
        {
            MouseButton.Left => Pointer2Button.Left,
            MouseButton.Right => Pointer2Button.Right,
            MouseButton.Middle => Pointer2Button.Middle,
            _ => result.Buttons
        };

        PointerUp?.Invoke(result);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs args)
    {
        var result = CreatePointerEvent(args);
        result.WheelDelta = args.Delta;
        WheelMove?.Invoke(result);
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
                (float)(ActualHeight * dpi.DpiScaleY));
        }
    }

    public new event EventHandler? SizeChanged;
    public event EventHandler? Ready;
    public event PointerEventDelegate? PointerDown;
    public event PointerEventDelegate? PointerUp;
    public event PointerEventDelegate? PointerMove;
    public event PointerEventDelegate? WheelMove;

    public virtual nint HWnd => 0;

    public abstract bool SupportsDualRender { get; }
}

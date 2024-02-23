using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Editor
{
    public struct PointerEvent
    {
        public float X;

        public float Y;

        public MouseButton Buttons;

        public readonly bool IsLeftDown => (Buttons & MouseButton.Left) == MouseButton.Left;

        public readonly bool IsMiddleDown => (Buttons & MouseButton.Middle) == MouseButton.Middle;

        public readonly bool IsRightDown => (Buttons & MouseButton.Right) == MouseButton.Right;

        public int WheelDelta { get; internal set; }
    }

    public delegate void PointerEventDelegate(PointerEvent ev);

    public interface IPointerEventSource
    {
        void CapturePointer();

        void ReleasePointer();

        event PointerEventDelegate PointerDown;

        event PointerEventDelegate PointerUp;

        event PointerEventDelegate PointerMove;
    }
}

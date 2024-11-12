using System.Numerics;

namespace XrInteraction
{
    [Flags]
    public enum Pointer2Button
    {
        None = 0x0, 
        Left = 0x1,
        Middle = 0x2,
        Right = 0x4,
        Trigger = Left,
        Squeeze = Right,
        A = 0x8,
        B = 0x10,
        Y = 0x20,
        C = 0x40,
    }

    public struct Pointer2Event
    {
        public Vector2 Position;

        public Pointer2Button Buttons;

        public int WheelDelta;

        public readonly bool IsLeftDown => (Buttons & Pointer2Button.Left) == Pointer2Button.Left;

        public readonly bool IsMiddleDown => (Buttons & Pointer2Button.Middle) == Pointer2Button.Middle;

        public readonly bool IsRightDown => (Buttons & Pointer2Button.Right) == Pointer2Button.Right;
    }
}

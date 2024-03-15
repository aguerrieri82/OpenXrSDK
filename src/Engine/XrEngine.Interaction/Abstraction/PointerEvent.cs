namespace XrEngine.Interaction
{
    [Flags]
    public enum PointerButton
    {
        Left = 0x1,
        Middle = 0x2,
        Right = 0x4
    }

    public struct PointerEvent
    {
        public float X;

        public float Y;

        public PointerButton Buttons;

        public int WheelDelta;

        public readonly bool IsLeftDown => (Buttons & PointerButton.Left) == PointerButton.Left;

        public readonly bool IsMiddleDown => (Buttons & PointerButton.Middle) == PointerButton.Middle;

        public readonly bool IsRightDown => (Buttons & PointerButton.Right) == PointerButton.Right;


    }
}

using System.Numerics;

namespace XrInteraction
{
    public struct InputButton
    {
        public bool IsDown;

        public bool IsChanged;
    }

    public interface ISurfaceInput
    {
        bool IsPointerValid { get; }

        IPointer? Pointer { get; }

        public Vector2 Position { get; }

        public InputButton MainButton { get; }

        public InputButton SecondaryButton { get; }
    }
}

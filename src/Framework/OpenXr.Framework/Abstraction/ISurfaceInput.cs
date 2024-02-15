using System.Numerics;

namespace OpenXr.Framework
{
    public struct InputButton
    {
        public bool IsDown;

        public bool IsChanged;
    }

    public interface ISurfaceInput
    {

        bool IsPointerValid { get; }

        public Vector2 Pointer { get; }

        public InputButton MainButton { get; }

        [Obsolete("Test")]
        public InputButton BackButton { get; }
    }
}

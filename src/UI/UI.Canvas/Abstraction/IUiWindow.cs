using System.Numerics;
using XrMath;

namespace CanvasUI
{
    public interface IUiWindow
    {
        public void Close();

        public UiElement? Content { get; set; }

        public Size2 Size { get; set; }

        public Vector3 Position { get; set; }
    }
}

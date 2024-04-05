using System.Numerics;
using XrMath;

namespace CanvasUI
{
    public interface IUiWindowManager
    {
        IUiWindow CreateWindow(Size2 size, Vector3 position, UiElement content);
    }
}

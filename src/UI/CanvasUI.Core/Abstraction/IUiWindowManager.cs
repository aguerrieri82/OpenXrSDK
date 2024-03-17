using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace CanvasUI
{
    public interface IUiWindowManager
    {
        IUiWindow CreateWindow(Size2 size, Vector3 position, UiElement content);
    }
}

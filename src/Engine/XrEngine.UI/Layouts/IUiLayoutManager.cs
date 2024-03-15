using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public interface IUiLayoutManager
    {
        Size2 Measure(Size2 availSize, object? layoutParams);

        Size2 Arrange(Rect2 finalRect, object? layoutParams);
    }
}

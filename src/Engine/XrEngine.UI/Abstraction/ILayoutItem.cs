using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.UI
{
    public interface ILayoutItem
    {
        Size2 Measure(Size2 size);

        Size2 Arrange(Rect2 finalRect);

        Size2 DesiredSize { get; }
    }
}

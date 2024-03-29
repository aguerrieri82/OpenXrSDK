﻿using XrMath;

namespace CanvasUI
{
    public interface IUiLayoutItem
    {
        Size2 Measure(Size2 size);

        Size2 Arrange(Rect2 finalRect);

        Size2 DesiredSize { get; }

        string? Name { get; }

    }
}

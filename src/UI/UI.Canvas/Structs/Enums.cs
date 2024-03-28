

namespace CanvasUI
{
    public enum UiLayoutType
    {
        Flex,
        Absolute
    }

    public enum UiAlignment
    {
        Start,
        Center,
        End,
        Stretch,
        SpaceBetween,
        SpaceAround,
    }

    public enum UiTextWrap
    {
        NoWrap,
        Whitespaces,
        BreakWord
    }

    public enum UiVisibility
    {
        Visible,
        Hidden,
        Collapsed
    }

    public enum UiOverflow
    {
        Visible,
        Hidden,
        Scroll
    }

    public enum UIOrientation
    {
        Horizontal,
        Vertical
    }

    public enum UiWrapMode
    {
        NoWrap,
        Wrap
    }

    public enum UiControlState
    {
        None = 0,
        Selected = 1,
        Focused = 2,
        Disabled = 4,
        Active = 8,
        Hover = 16
    }

    public enum UiValueReference
    {
        None,
        ParentWidth,
        ParentHeight,
        ParentFontSize,
        FontSize
    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Interaction;

namespace XrEngine.UI
{

    public delegate void UiEventHandler<T>(UiComponent sender, T uiEvent) where T : UiEvent;    

    public enum UiEventType
    {
        GotFocus,
        LostFocus,
        PointerDown,
        PointerUp,
        PointerMove,
        PointerEnter,
        PointerLeave
    }

    public enum UiEventDispatch
    {
        Direct,
        Bubble,
        Tunnel
    }


    public class UiEvent
    {
        public bool IsHandled;

        public UiEventType Type;
    }

    public class UiRoutedEvent : UiEvent
    {
        public UiComponent? Source;

        public UiEventDispatch Dispatch;

        public bool StopBubble; 
    }

    public class UiPointerEvent : UiRoutedEvent
    {

        public Vector2 ScreenPosition;

        public PointerButton Buttons;

        public int WheelDelta;
    }
}

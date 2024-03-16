using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Interaction;

namespace XrEngine.UI
{
    public static class UiFocusManager
    {
        private static UiComponent? _activeFocus;
        private static UiComponent? _hoverElement;

        public static void SetHoverElement(UiComponent? element, Vector2 screenPos, PointerButton buttons)
        {
            if (element == _hoverElement)
                return;

            var curParents = _hoverElement.VisualAncestorsAndSelf().ToArray();

            var newParents = element.VisualAncestorsAndSelf().ToArray();

            foreach (var item in curParents)
            {
                if (!newParents.Contains(item))
                    item.DispatchEvent(new UiPointerEvent
                    {
                        Buttons = buttons,
                        ScreenPosition = screenPos,
                        Dispatch = UiEventDispatch.Direct,
                        Source = item,
                        Type = UiEventType.PointerLeave
                    });
            }

            foreach (var item in newParents)
            {
                if (!curParents.Contains(item))
                    item.DispatchEvent(new UiPointerEvent
                    {
                        Buttons = buttons,
                        ScreenPosition = screenPos,
                        Dispatch = UiEventDispatch.Direct,
                        Source = item,
                        Type = UiEventType.PointerEnter
                    });
            }

            _hoverElement = element;
        }


        public static void SetFocus(UiComponent? element)
        {
            if (_activeFocus == element)
                return;
            
            _activeFocus?.DispatchEvent(new UiRoutedEvent 
            { 
                Source = _activeFocus, 
                Type = UiEventType.LostFocus, 
                Dispatch = UiEventDispatch.Bubble 
            });

            _activeFocus = element;

            _activeFocus?.DispatchEvent(new UiRoutedEvent
            {
                Source = _activeFocus,
                Type = UiEventType.GotFocus,
                Dispatch = UiEventDispatch.Bubble
            });
        }


        public static UiComponent? ActiveFocus => _activeFocus;
    }
}

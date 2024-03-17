using System.Numerics;

namespace CanvasUI
{
    public static class UiManager
    {
        private static UiElement? _activeFocus;
        private static UiElement? _hoverElement;
        private static Dictionary<int, UiElement?> _pointerCaptures = [];


        public static void SetPointerCapture(int pointerId, UiElement? element)
        {
            _pointerCaptures[pointerId] = element;
        }

        public static UiElement? GetPointerCapture(int pointerId)
        {
            if (_pointerCaptures.TryGetValue(pointerId, out UiElement? element))
                return element;
            return null;
        }

        public static void SetHoverElement(UiElement? element, Vector2 screenPos, UiPointerButton buttons)
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
                        WindowPosition = screenPos,
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
                        WindowPosition = screenPos,
                        Dispatch = UiEventDispatch.Direct,
                        Source = item,
                        Type = UiEventType.PointerEnter
                    });
            }

            _hoverElement = element;
        }


        public static void SetFocus(UiElement? element)
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


        public static UiElement? ActiveFocus => _activeFocus;

        public static IUiWindowManager? WindowManager { get; set; }
    }
}

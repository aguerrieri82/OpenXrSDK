using System.Numerics;

namespace CanvasUI
{
    public static class UiManager
    {
        private static UiElement? _activeFocus;
        private static UiElement? _hoverElement;
        private static readonly Dictionary<int, UiElement?> _pointerCaptures = [];
        private static readonly Dictionary<Type, Queue<UiEvent>> _eventPool = [];


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
                {
                    var ev = AcquireEvent<UiPointerEvent>();
                    ev.Buttons = buttons;
                    ev.WindowPosition = screenPos;
                    ev.Dispatch = UiEventDispatch.Direct;
                    ev.Source = item;
                    ev.Type = UiEventType.PointerLeave;
                    item.DispatchEvent(ev);
                }
            }

            foreach (var item in newParents)
            {
                if (!curParents.Contains(item))
                {
                    var ev = AcquireEvent<UiPointerEvent>();
                    ev.Buttons = buttons;
                    ev.WindowPosition = screenPos;
                    ev.Dispatch = UiEventDispatch.Direct;
                    ev.Source = item;
                    ev.Type = UiEventType.PointerEnter;
                    item.DispatchEvent(ev);
                }
            }

            _hoverElement = element;
        }


        public static void SetFocus(UiElement? element)
        {
            if (_activeFocus == element)
                return;


            if (_activeFocus != null)
            {
                var ev = AcquireEvent<UiRoutedEvent>();
                ev.Source = _activeFocus;
                ev.Type = UiEventType.LostFocus;
                ev.Dispatch = UiEventDispatch.Bubble;
                _activeFocus.DispatchEvent(ev);
            }

            _activeFocus = element;

            if (_activeFocus != null)
            {
                var ev = AcquireEvent<UiRoutedEvent>();
                ev.Source = _activeFocus;
                ev.Type = UiEventType.GotFocus;
                ev.Dispatch = UiEventDispatch.Bubble;
                _activeFocus.DispatchEvent(ev);
            }
        }

        public static T AcquireEvent<T>() where T : UiEvent, new()
        {
            if (!_eventPool.TryGetValue(typeof(T), out var pool))
            {
                pool = [];
                _eventPool[typeof(T)] = pool;
            }

            if (pool.Count == 0)
                pool.Enqueue(new T());

            var res = (T)pool.Dequeue();
            res.Timestamp = DateTime.Now.Ticks;
            return res;
        }

        public static void ReleaseEvent(UiEvent ev)
        {
            var pool = _eventPool[ev.GetType()];
            if (!pool.Contains(ev))
                pool.Enqueue(ev);
        }

        public static UiElement? ActiveFocus => _activeFocus;

        public static IUiWindowManager? WindowManager { get; set; }
    }
}

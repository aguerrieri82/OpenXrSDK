using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI.Components
{
    public static class UiFocusManager
    {
        private static UiComponent? _activeFocus;

        public static void SetFocus(UiComponent? element)
        {
            if (_activeFocus == element)
                return;
            
            if (_activeFocus != null)
                _activeFocus.DispatchEvent(new UiRoutedEvent { Source = _activeFocus, Type = UiEventType.LostFocus });

            _activeFocus = element;

            if (_activeFocus != null)
                _activeFocus.DispatchEvent(new UiRoutedEvent { Source = _activeFocus, Type = UiEventType.GotFocus });
        }


        public static UiComponent? ActiveFocus => _activeFocus;
    }
}

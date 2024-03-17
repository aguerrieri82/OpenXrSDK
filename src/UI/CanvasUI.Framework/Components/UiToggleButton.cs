using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace CanvasUI
{
    public class UiToggleButton : UiContentView
    {
        public UiToggleButton()
        {
            IsFocusable = true;
        }

        protected override void OnPointerUp(UiPointerEvent ev)
        {
            IsChecked = !IsChecked;
            base.OnPointerUp(ev);
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            if (propName == nameof(IsChecked))
                CheckedChange?.Invoke(this, EventArgs.Empty);

            base.OnPropertyChanged(propName, value, oldValue);
        }

        [UiProperty(false)]
        public bool IsChecked
        {
            get => GetValue<bool>(nameof(IsChecked))!;
            set => SetValue(nameof(IsChecked), value);
        }

        public event EventHandler? CheckedChange;
    }
}

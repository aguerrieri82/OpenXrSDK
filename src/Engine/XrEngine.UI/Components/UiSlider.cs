using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;

namespace XrEngine.UI
{
    public class UiSlider : UiComponent
    {

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty); 
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            if (propName == nameof(Value))
                OnValueChanged();

            base.OnPropertyChanged(propName, value, oldValue);
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            throw new NotImplementedException();
        }


        [UiProperty(1f)]
        public float Max
        {
            get => GetValue<float>(nameof(Max))!;
            set => SetValue(nameof(Max), value);
        }


        [UiProperty(0f)]
        public float Min
        {
            get => GetValue<float>(nameof(Min))!;
            set => SetValue(nameof(Min), value);
        }

        [UiProperty(0f)]
        public float Value
        {
            get => GetValue<float>(nameof(Value))!;
            set => SetValue(nameof(Value), value);
        }

        [UiProperty(0f)]
        public float Step
        {
            get => GetValue<float>(nameof(Step))!;
            set => SetValue(nameof(Step), value);
        }

        public event EventHandler? ValueChanged;
    }
}

using SkiaSharp;


namespace CanvasUI
{
    public class UiSlider : UiElement, IInputElement<float>
    {
        public UiSlider()
        {
            IsFocusable = true;
        }

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
            var middleY = _clientRect.Y + _clientRect.Height / 2;
            var height = 8;
            var rect = new SKRect(_clientRect.X, middleY - height / 2, _clientRect.Width, height);

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

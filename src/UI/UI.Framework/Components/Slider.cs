using SkiaSharp;
using System.Numerics;
using XrMath;


namespace CanvasUI
{
    public struct SliderStyle
    {
        public float BarHeight;

        public Color BarColor;

        public float ThumbRadius;

        public Color ThumbColor;
    }

    public class Slider : UiElement, IInputElement<float>
    {
        SliderStyle _style;
        private bool _isMoving;

        public Slider()
        {
            IsFocusable = true;

            Style.OverflowX = UiOverflow.Visible;

            _style = new SliderStyle
            {
                BarHeight = 6,
                BarColor = "#ccc",
                ThumbColor = "#ccf",
                ThumbRadius = 12
            };

        }

        protected virtual void OnValueChanged(float value, float oldValue)
        {
            ValueChanged?.Invoke(this, value, oldValue);
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            if (propName == nameof(Value))
                OnValueChanged((float)value!, (float)oldValue!);

            base.OnPropertyChanged(propName, value, oldValue);
        }

        protected override Size2 MeasureWork(Size2 availSize)
        {
            return new Size2(availSize.Width, _style.ThumbRadius * 2);
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            var pos = GetThumbPos();

            var bar = GetBarRect();

            canvas.DrawRect(bar.ToSKRect(), SKResources.FillColor(_style.BarColor));

            canvas.DrawCircle(pos.X, pos.Y, _style.ThumbRadius, SKResources.FillColor(_style.ThumbColor));
        }

        protected Rect2 GetBarRect()
        {
            var pos = GetThumbPos();
            return new Rect2(_clientRect.X, pos.Y - _style.BarHeight / 2, _clientRect.Width, _style.BarHeight);
        }

        protected Vector2 GetThumbPos()
        {
            return new Vector2(
                _clientRect.X + (Value - Min) / MathF.Abs(Max - Min) * (_clientRect.Width),
                _clientRect.Y + _clientRect.Height / 2);
        }

        protected float PosToValue(Vector2 windowPos)
        {
            var relX = (windowPos.X - _clientRect.X) / _clientRect.Width;

            var value = Min + relX * MathF.Abs(Max - Min);

            if (Step != 0)
                value = MathF.Round(value / Step) * Step;

            return MathF.Max(Min, MathF.Min(Max, value));
        }

        protected override void OnPointerDown(UiPointerEvent ev)
        {
            var pos = GetThumbPos();
            var rect = GetBarRect();

            if ((ev.WindowPosition - pos).Length() < _style.ThumbRadius)
            {
                ev.Pointer!.Capture(this);
                _isMoving = true;
            }
            else if (ev.WindowPosition.Y >= rect.Top && ev.WindowPosition.Y <= rect.Bottom)
            {
                Value = PosToValue(ev.WindowPosition);
            }

            base.OnPointerDown(ev);
        }


        protected override void OnPointerMove(UiPointerEvent ev)
        {
            if (_isMoving)
                Value = PosToValue(ev.WindowPosition);
            base.OnPointerMove(ev);
        }

        protected override void OnPointerUp(UiPointerEvent ev)
        {
            if (_isMoving)
            {
                _isMoving = false;
                ev.Pointer!.Release();
            }

            base.OnPointerUp(ev);
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

        public event InputValueChangedHandler<float>? ValueChanged;
    }
}

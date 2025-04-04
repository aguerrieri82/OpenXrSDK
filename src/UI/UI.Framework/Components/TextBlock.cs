﻿using SkiaSharp;
using XrMath;

namespace CanvasUI
{
    public class TextBlock : UiElement
    {
        TextLayoutManager.Layout _lastLayout;
        TextLayoutManager.LayoutParams _layoutParams;

        public TextBlock()
        {
        }

        protected void OnTextChanged(string? value, string? oldValue)
        {
            _lastLayout.CurrentSize.Width = -1;
            InvalidateLayout();
        }

        protected override Size2 MeasureWork(Size2 availSize)
        {
            TextLayoutManager.ExtractLayoutParams(ActualStyle, ref _layoutParams);

            _lastLayout = TextLayoutManager.Arrange(availSize, _layoutParams, Text);

            return _lastLayout.CurrentSize;
        }

        protected override Size2 ArrangeWork(Rect2 finalRect)
        {
            if (finalRect.Size != _lastLayout.AvailSize)
                _lastLayout = TextLayoutManager.Arrange(finalRect.Size, _layoutParams, Text);

            return _lastLayout.CurrentSize;
        }

        protected override void DrawWork(SKCanvas canvas)
        {
            var color = ActualStyle.Color.Value;
            var paint = SKResources.FillColor(color!.Value);

            foreach (var line in _lastLayout.Lines)
                canvas.DrawText(line.Text, _contentRect.X + line.Position.X, _contentRect.Y + line.Position.Y - _lastLayout.Font.Metrics.Descent, _lastLayout.Font, paint);
        }

        [UiProperty("", UiPropertyFlags.Layout)]
        public string Text
        {
            get => GetValue<string>(nameof(Text))!;
            set => SetValue(nameof(Text), value);
        }

    }
}

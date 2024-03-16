using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Components;
using XrMath;

namespace XrEngine.UI
{
    public class UiTextBlock : UiComponent
    {
        protected struct TextLine
        {
            public string Text;

            public Vector2 Position;

            public float Width;
        }

        protected struct TextLayout
        {
            public Size2 AvailSize;

            public Size2 CurrentSize;

            public float LineSize;

            public SKFont Font;

            public TextLine[] Lines;
        }

        TextLayout _lastLayout;

        public UiTextBlock()
        {
        }

        protected TextLayout LayoutText(Size2 availSize)
        {
            var font = ActualStyle.GetFont();
            var lineSize = ActualStyle.LineSize.ToPixel(this, UiValueReference.ParentFontSize);
            var alignment = ActualStyle.TextAlign.Value;
            var wrap = ActualStyle.TextWrap.Value;

            var result = new TextLayout();
            result.Font = font;
            result.LineSize = lineSize;
            result.AvailSize = availSize;   

            int i = 0;
            var text = Text.AsSpan();

            var curLine = new StringBuilder();

            var lastBreakPoint = -1;

            float curY = 0;

            var lines = new List<TextLine>();   

            void NewLine()
            {
                curY += lineSize;

                if (curLine.Length > 0 && curLine[^1] == ' ')
                    curLine.Length--;

                var newLine = new TextLine
                {
                    Text = curLine.ToString(),
                    Position = new Vector2(0, curY),
                };

                newLine.Width = font.MeasureText(newLine.Text);

                lines.Add(newLine);

                lastBreakPoint = -1;
                curLine.Length = 0;

                result.CurrentSize.Width = MathF.Max(newLine.Width, result.CurrentSize.Width);
            }

            while (i < text.Length)
            {
                var c = text[i];

                var isWhite = char.IsWhiteSpace(c);

                switch (c)
                {
                    case '\n':
                        NewLine();
                        break;
                    case '\t':
                        curLine.Append("   ");
                        break;
                    case '\r':
                        break;
                    default:
                        curLine.Append(c);
                        break;
                }


                if (isWhite || wrap == UiTextWrap.BreakWord)
                {
                    var curWidth = font.MeasureText(curLine.ToString());

                    if (curWidth > availSize.Width)
                    {
                        if (wrap == UiTextWrap.BreakWord)
                            NewLine();
                        else if (wrap == UiTextWrap.Whitespaces)
                        {
                            var missText = string.Empty;
                            if (lastBreakPoint != -1)
                            {
                                missText = curLine.ToString().Substring(lastBreakPoint);
                                curLine.Length = lastBreakPoint;
                            }
                            NewLine();
                            curLine.Append(missText);
                        }
                    }
                    else
                    {
                        lastBreakPoint = curLine.Length;
                    }
                }

                i++;
            }

            if (curLine.Length > 0)
                NewLine();

            result.CurrentSize.Height = curY;
            result.Lines = lines.ToArray();

            if (alignment != UiAlignment.Start)
            {
                foreach (ref var line in result.Lines.AsSpan())
                {
                    if (alignment == UiAlignment.Center)
                        line.Position.X = (result.CurrentSize.Width - line.Width) / 2;
                    else
                        line.Position.X = result.CurrentSize.Width - line.Width;
                }
            }

            return result;
        }

        protected override void OnPropertyChanged(string propName, object? value, object? oldValue)
        {
            if (propName == nameof(Text))
            {
                _lastLayout.CurrentSize.Width = -1;
                InvalidateLayout();
            }

            base.OnPropertyChanged(propName, value, oldValue);
        }

        protected override Size2 MeasureWork(Size2 availSize)
        {
            _lastLayout = LayoutText(availSize);
            return _lastLayout.CurrentSize;
        }

        protected override Size2 ArrangeWork(Rect2 finalRect)
        {
            if (finalRect.Size != _lastLayout.AvailSize)
                _lastLayout = LayoutText(finalRect.Size);

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

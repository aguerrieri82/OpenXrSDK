using SkiaSharp;
using System.Globalization;
using System.Numerics;
using System.Text;
using XrMath;

namespace CanvasUI
{
    public static class TextLayoutManager
    {
        public struct LayoutLine
        {
            public string Text;

            public Vector2 Position;

            public float Width;
        }

        public struct Layout
        {
            public Size2 AvailSize;

            public Size2 CurrentSize;

            public float LineSize;

            public SKFont Font;

            public LayoutLine[] Lines;
        }

        public struct LayoutParams
        {
            public SKFont Font;

            public float LineSize;

            public UiAlignment Alignment;

            public UiTextWrap Wrap;
        }

        public static void ExtractLayoutParams(UiStyle style, ref LayoutParams result)
        {
            result.Font = style.GetFont();
            result.LineSize = style.LineSize.ToPixel(style.Owner, UiValueReference.FontSize);
            result.Alignment = style.TextAlign.Value;
            result.Wrap = style.TextWrap.Value;
        }

        public static Layout Arrange(Size2 availSize, LayoutParams lp, string textStr)
        {
            var result = new Layout
            {
                Font = lp.Font,
                LineSize = lp.LineSize,
                AvailSize = availSize
            };

            var i = 0;
            var text = textStr.AsSpan();

            var curLine = new StringBuilder();

            float curWidth = 0;

            float curY = 0;

            var lines = new List<LayoutLine>();

            void NewLine()
            {
                curY += lp.LineSize;

                while (curLine.Length > 0 && curLine[^1] == ' ')
                    curLine.Length--;

                var newLine = new LayoutLine
                {
                    Text = curLine.ToString(),
                    Position = new Vector2(0, curY),
                };

                newLine.Width = lp.Font.MeasureText(newLine.Text);

                lines.Add(newLine);

                curWidth = 0;
                curLine.Length = 0;

                result.CurrentSize.Width = MathF.Max(newLine.Width, result.CurrentSize.Width);
            }

            while (i < text.Length)
            {
                if (text[i] == '\r')
                {
                    i++;
                    continue;
                }

                if (text[i] == '\n')
                {
                    NewLine();
                    i++;
                    continue;
                }

                var start = i;
                var isWhite = char.IsWhiteSpace(text[i]);

                if (lp.Wrap == UiTextWrap.BreakWord)
                    i += StringInfo.GetNextTextElementLength(text[i..]);
                else
                {
                    while (i < text.Length && text[i] != '\n' && text[i] != '\r' && text[i] != '\t')
                    {
                        if (lp.Wrap == UiTextWrap.Whitespaces && char.IsWhiteSpace(text[i]) != isWhite)
                            break;

                        i++;
                    }
                }

                var part = text[start..i];

                if (text[start] == '\t')
                {
                    part = "   ".AsSpan();
                    i = start + 1;
                }

                if (lp.Wrap != UiTextWrap.NoWrap)
                {
                    var width = lp.Font.MeasureText(part);

                    if (curLine.Length > 0 && curWidth + width > availSize.Width &&
                        (lp.Wrap == UiTextWrap.BreakWord || !isWhite))
                        NewLine();

                    curWidth += width;
                }

                curLine.Append(part);
            }

            if (curLine.Length > 0)
                NewLine();

            result.CurrentSize.Height = curY;
            result.Lines = lines.ToArray();

            if (lp.Alignment != UiAlignment.Start)
            {
                foreach (ref var line in result.Lines.AsSpan())
                {
                    if (lp.Alignment == UiAlignment.Center)
                        line.Position.X = (result.CurrentSize.Width - line.Width) / 2;
                    else
                        line.Position.X = result.CurrentSize.Width - line.Width;
                }
            }

            return result;
        }
    }
}

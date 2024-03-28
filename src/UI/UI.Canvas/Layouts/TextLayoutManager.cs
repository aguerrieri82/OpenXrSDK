using SkiaSharp;
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

            int i = 0;
            var text = textStr.AsSpan();

            var curLine = new StringBuilder();

            var lastBreakPoint = -1;

            float curY = 0;

            var lines = new List<LayoutLine>();

            void NewLine()
            {
                curY += lp.LineSize;

                if (curLine.Length > 0 && curLine[^1] == ' ')
                    curLine.Length--;

                var newLine = new LayoutLine
                {
                    Text = curLine.ToString(),
                    Position = new Vector2(0, curY),
                };

                newLine.Width = lp.Font.MeasureText(newLine.Text);

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


                if (isWhite || lp.Wrap == UiTextWrap.BreakWord)
                {
                    var curWidth = lp.Font.MeasureText(curLine.ToString());

                    if (curWidth > availSize.Width)
                    {
                        if (lp.Wrap == UiTextWrap.BreakWord)
                            NewLine();
                        else if (lp.Wrap == UiTextWrap.Whitespaces)
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

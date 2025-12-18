using SkiaSharp;
using System.Reflection;
using XrMath;

namespace CanvasUI
{
    public static class SKResources
    {
        static readonly Dictionary<string, SKPaint> _paints = [];
        static readonly Dictionary<string, SKTypeface> _typefaces = [];
        static readonly Dictionary<string, SKFont> _fonts = [];

        public static SKTypeface TypefaceFromRes(string resName)
        {
            if (!_typefaces.TryGetValue(resName, out SKTypeface? result))
            {
                Assembly assembly = Assembly.GetCallingAssembly();

                string? name = assembly.GetManifestResourceNames().Where(a => a.Contains(resName)).FirstOrDefault();
                if (name == null)
                    throw new NotSupportedException();

                using (Stream? stream = assembly.GetManifestResourceStream(name))
                    result = SKFontManager.Default.CreateTypeface(stream);

                _typefaces[resName] = result;
            }

            return result;
        }

        public static SKTypeface Typeface(string familyName)
        {
            if (!_typefaces.TryGetValue(familyName, out SKTypeface? result))
            {
                result = SKTypeface.FromFamilyName(familyName);
                _typefaces[familyName] = result;
            }
            return result;
        }

        public static SKFont Font(SKTypeface typeface, float size)
        {
            string id = string.Concat("font_", typeface.FamilyName, "_", size);

            if (!_fonts.TryGetValue(id, out SKFont? result))
            {
                result = new SKFont(typeface, size);
                result.Subpixel = true;
                _fonts[id] = result;
            }
            return result;
        }

        public static SKFont Font(string family, float size)
        {
            return Font(Typeface(family), size);
        }

        public static SKPaint FillColor(Color color)
        {
            string id = "fill_" + color.ToString();
            if (!_paints.TryGetValue(id, out SKPaint? paint))
            {
                paint = new SKPaint();
                paint.ColorF = new SKColorF(color.R, color.G, color.B, color.A);
                paint.Style = SKPaintStyle.Fill;
                _paints[id] = paint;
            }
            return paint;
        }

        public static SKPaint Stroke(Color color, float width)
        {
            string id = string.Concat("stroke_", color.ToString(), "_", width);
            if (!_paints.TryGetValue(id, out SKPaint? paint))
            {
                paint = new();
                paint.ColorF = new SKColorF(color.R, color.G, color.B, color.A);
                paint.StrokeWidth = width;
                paint.Style = SKPaintStyle.Stroke;
                paint.IsStroke = true;
                paint.IsAntialias = false;
                paint.StrokeJoin = SKStrokeJoin.Miter;
                _paints[id] = paint;
            }
            return paint;
        }

        public static SKPaint Stroke(Color color, float width, float dashSize)
        {
            string id = string.Concat("stroke_", color.ToString(), "_", width, "_", dashSize);
            if (!_paints.TryGetValue(id, out SKPaint? paint))
            {
                paint = new();
                paint.ColorF = new SKColorF(color.R, color.G, color.B, color.A);
                paint.StrokeWidth = width;
                paint.Style = SKPaintStyle.Stroke;
                paint.IsStroke = true;
                paint.IsAntialias = false;
                paint.PathEffect = SKPathEffect.CreateDash([dashSize, dashSize], dashSize * 2);
                paint.StrokeJoin = SKStrokeJoin.Miter;
                _paints[id] = paint;
            }
            return paint;
        }
    }
}

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
            if (!_typefaces.TryGetValue(resName, out var result))
            {
                var assembly = Assembly.GetCallingAssembly();

                var name = assembly.GetManifestResourceNames().Where(a => a.Contains(resName)).FirstOrDefault();
                if (name == null)
                    throw new NotSupportedException();

                using (var stream = assembly.GetManifestResourceStream(name))
                    result = SKFontManager.Default.CreateTypeface(stream);

                _typefaces[resName] = result;
            }

            return result;
        }

        public static SKTypeface Typeface(string familyName)
        {
            if (!_typefaces.TryGetValue(familyName, out var result))
            {
                result = SKTypeface.FromFamilyName(familyName);
                _typefaces[familyName] = result;
            }
            return result;
        }

        public static SKFont Font(SKTypeface typeface, float size)
        {
            var id = string.Concat("font_", typeface.FamilyName, "_", size);

            if (!_fonts.TryGetValue(id, out var result))
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
            var id = "fill_" + color.ToString();
            if (!_paints.TryGetValue(id, out var paint))
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
            var id = string.Concat("stroke_", color.ToString(), "_", width);
            if (!_paints.TryGetValue(id, out var paint))
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
    }
}

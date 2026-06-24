using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Numerics;
using System.Text;
using XrMath;

namespace XrEngine
{
    public enum Align
    {
        Start,
        Center,
        End
    }

    public enum DrawUnit
    {
        Pixel,
        Points,
        Uv,
        World,
        Local
    }

    public struct UnitValue
    {
        public UnitValue()
        {

        }

        public static implicit operator float (UnitValue value)
        {
            return value.Value;
        }

        public static implicit operator UnitValue(float value)
        {
            return new UnitValue() {  Value = value, Unit = DrawUnit.Pixel };
        }
        

        public DrawUnit Unit;

        public float Value;
    }

    public class ScreenCanvas 
    {
        private Size2I _size;
        private Camera? _camera;
        private SKCanvas? _canvas;
        private Dictionary<float, SKFont> _fonts = [];
        private Dictionary<Color, SKPaint> _fills = [];
        private Dictionary<string, SKPaint> _strokes = [];

        public ScreenCanvas()
        {
  
        }

        public SKPoint ToScreen(Vector3 point)
        {
            Debug.Assert(_camera != null);

            var viewProj = _camera.Eyes != null && _camera.Eyes.Length > 1
                ? _camera.Eyes[_camera.ActiveEye].ViewProj
                : _camera.ViewProjection;

            var clip = Vector4.Transform(new Vector4(point, 1.0f), viewProj);

            if (MathF.Abs(clip.W) < 1e-8f)
                return new Vector2(float.NaN, float.NaN);

            var invW = 1.0f / clip.W;

            var ndcX = clip.X * invW;
            var ndcY = clip.Y * invW;

            return new SKPoint(
                (ndcX * 0.5f + 0.5f) * _size.Width,
                (0.5f - ndcY * 0.5f) * _size.Height);

        }

        protected SKFont GetFont(float size)
        {
            if (!_fonts.TryGetValue(size, out var font))
            {
                font = new SKFont(SKTypeface.Default, size);
                _fonts[size] = font;
            }
            return font;
        }

        protected SKColor ToSkColor(Color color)
        {
            return new SKColor((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255), (byte)(color.A * 255));
        }

            protected SKPaint GetFill(Color color)
            {
                if (!_fills.TryGetValue(color, out var paint))
                {
                    paint = new SKPaint
                    {
                        Color = ToSkColor(color),
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    _fills[color] = paint;
                }

                return paint;
            }

        protected SKPaint GetStroke(Color color, float strokeSize)
        {
            var key = $"{color}|{strokeSize}";

            if (!_strokes.TryGetValue(key, out var paint))
            {
                paint = new SKPaint
                {
                    Color = ToSkColor(color),
                    StrokeWidth = strokeSize,
                    Style = SKPaintStyle.Stroke,
                    IsAntialias = true
                };
                _strokes[key] = paint;
            }

            return paint;
        }

        public void DrawText(string text, Vector3 point, float size, Color color, Align align = Align.Center)
        {
            _canvas!.DrawText(text, ToScreen(point), (SKTextAlign)align, GetFont(size), GetFill(color));
        }

        public void Draw(Triangle3 triangle, Color fillColor, Color strokeColor, float strokeSize)
        {
            using var builder = new SKPathBuilder();
            
            builder.AddPoly([ToScreen(triangle.V0), ToScreen(triangle.V1), ToScreen(triangle.V2)], true);
            
            var path = builder.Snapshot();

            _canvas!.DrawPath(builder.Snapshot(), GetFill(fillColor));
            if (strokeSize > 0)
                _canvas!.DrawPath(builder.Snapshot(), GetStroke(strokeColor, strokeSize));
        }

        public void Draw(Line3 line, Color strokeColor, float strokeSize)
        {
            _canvas!.DrawLine(ToScreen(line.From), ToScreen(line.To), GetStroke(strokeColor, strokeSize));
        }

        public void Configure(SKCanvas canvas, Camera camera, Size2I size)
        {
            _canvas = canvas;   
            _camera= camera;
            _size = size;
        }

        public SKCanvas Canvas => _canvas;

        public Camera Camera => _camera;

        public Size2I Size => _size;
    }
}

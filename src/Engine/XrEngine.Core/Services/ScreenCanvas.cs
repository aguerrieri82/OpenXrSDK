using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
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
        Uv,
        WorldY,
        LocalY
    }

    public struct UnitValue
    {
        public UnitValue()
        {
        }

        public UnitValue(float value, DrawUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        public static implicit operator float(UnitValue value)
        {
            return value.Value;
        }

        public static implicit operator UnitValue(float value)
        {
            return new UnitValue(value, DrawUnit.Pixel);
        }

        public static UnitValue Pixel(float value)
        {
            return new UnitValue(value, DrawUnit.Pixel);
        }

        public static UnitValue Uv(float value)
        {
            return new UnitValue(value, DrawUnit.Uv);
        }

        public static UnitValue WorldY(float value)
        {
            return new UnitValue(value, DrawUnit.WorldY);
        }

        public static UnitValue LocalY(float value)
        {
            return new UnitValue(value, DrawUnit.LocalY);
        }

        public DrawUnit Unit;

        public float Value;
    }

    public struct UnitPoint
    {
        public UnitPoint()
        {
        }

        public UnitPoint(Vector2 value, DrawUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        public UnitPoint(float x, float y, DrawUnit unit)
        {
            Value = new Vector2(x, y);
            Unit = unit;
        }

        public static implicit operator Vector2(UnitPoint value)
        {
            return value.Value;
        }

        public static implicit operator UnitPoint(Vector2 value)
        {
            return new UnitPoint(value, DrawUnit.Pixel);
        }

        public static UnitPoint Pixel(Vector2 value)
        {
            return new UnitPoint(value, DrawUnit.Pixel);
        }

        public static UnitPoint Pixel(float x, float y)
        {
            return new UnitPoint(x, y, DrawUnit.Pixel);
        }

        public static UnitPoint Uv(Vector2 value)
        {
            return new UnitPoint(value, DrawUnit.Uv);
        }

        public static UnitPoint Uv(float x, float y)
        {
            return new UnitPoint(x, y, DrawUnit.Uv);
        }

        public DrawUnit Unit;

        public Vector2 Value;
    }

    public class ScreenCanvas
    {
        private Size2I _size;
        private int _activeEye;
        private Camera? _camera;
        private SKCanvas? _canvas;
        private readonly Dictionary<float, SKFont> _fonts = [];
        private readonly Dictionary<Color, SKPaint> _fills = [];
        private readonly Dictionary<string, SKPaint> _strokes = [];
        private float _scaleX = 1.0f;
        private float _scaleY = 1.0f;
        private Vector3 _uiOrigin;
        private Vector3 _uiAxisX;
        private Vector3 _uiAxisY;
        private bool _hasUiPlane;
        private float _distance;

        public ScreenCanvas()
        {
            Transform3 = Matrix4x4.Identity;
            Distance = 1.5f;
            Padding = new Vector2(0.1f, 0.1f);
            FontFamily = OperatingSystem.IsAndroid() ? "Roboto" : "Segoe UI";
        }

        public SKPoint ToScreen(UnitPoint point)
        {
            if (!TryToScreen(point, out var result))
                return new SKPoint(float.NaN, float.NaN);

            return result;
        }

        public SKPoint ToScreen(Vector3 point)
        {
            if (!TryToScreen(point, out var result))
                return new SKPoint(float.NaN, float.NaN);

            return result;
        }

        public float ToScreenSize(UnitValue value, UnitPoint point)
        {
            if (!TryToScreenSize(value, point, out var result))
                return float.NaN;

            return result;
        }

        public float ToScreenSize(UnitValue value, Vector3 point)
        {
            if (!TryToScreenSize(value, point, out var result))
                return float.NaN;

            return result;
        }

        public Vector2 ToUvNoPad(UnitPoint value)
        {
            TryToUvNoPad(value, out var result);
            return result;
        }

        protected bool TryToUvNoPad(UnitPoint value, out Vector2 uv)
        {
            Debug.Assert(_camera != null);

            var viewSize = _camera.ViewSize;

            switch (value.Unit)
            {
                case DrawUnit.Pixel:
                    uv = new Vector2(
                        value.Value.X / viewSize.Width,
                        value.Value.Y / viewSize.Height);
                    return true;

                case DrawUnit.Uv:
                    uv = value.Value;
                    return true;

                default:
                    uv = Vector2.Zero;
                    return false;
            }
        }

        protected bool TryToScreen(UnitPoint point, out SKPoint result)
        {
            result = new SKPoint(float.NaN, float.NaN);

            if (!_hasUiPlane)
                return false;

            if (!TryToUvNoPad(point, out var uv))
                return false;

            uv = new Vector2(
                Padding.X + uv.X * (1.0f - Padding.X * 2.0f),
                Padding.Y + uv.Y * (1.0f - Padding.Y * 2.0f));

            var worldPoint =
                _uiOrigin +
                _uiAxisX * uv.X +
                _uiAxisY * uv.Y;

            return TryToScreen(worldPoint, false, out result);
        }

        protected bool TryToScreen(Vector3 point, out SKPoint result)
        {
            return TryToScreen(point, true, out result);
        }

        protected bool TryToScreen(Vector3 point, bool applyTransform, out SKPoint result)
        {
            Debug.Assert(_camera != null);

            if (applyTransform && !Transform3.IsIdentity)
                point = point.Transform(Transform3);

            var viewProj = _camera.Eyes != null && _camera.Eyes.Length > 1
                ? _camera.Eyes[_activeEye].ViewProj
                : _camera.ViewProjection;

            var clip = Vector4.Transform(new Vector4(point, 1.0f), viewProj);

            if (clip.W <= 1e-8f)
            {
                result = new SKPoint(float.NaN, float.NaN);
                return false;
            }


            var invW = 1.0f / clip.W;

            var ndcX = clip.X * invW;
            var ndcY = clip.Y * invW;

            var viewSize = _camera.ViewSize;

            result = new SKPoint(
                (ndcX * 0.5f + 0.5f) * viewSize.Width,
                (0.5f - ndcY * 0.5f) * viewSize.Height);

            return true;
        }

        protected bool TryToScreenSize(UnitValue value, UnitPoint point, out float result)
        {
            Debug.Assert(_camera != null);

            var viewSize = _camera.ViewSize;

            Vector2 uv;

            switch (point.Unit)
            {
                case DrawUnit.Pixel:
                    uv = new Vector2(
                        point.Value.X / viewSize.Width,
                        point.Value.Y / viewSize.Height);
                    break;

                case DrawUnit.Uv:
                    uv = point;
                    break;

                default:
                    result = 0;
                    return false;
            }

            float uvSize;

            switch (value.Unit)
            {
                case DrawUnit.Pixel:
                    uvSize = value.Value / viewSize.Height;
                    break;

                case DrawUnit.Uv:
                    uvSize = value.Value;
                    break;

                default:
                    result = 0;
                    return false;
            }

            if (!TryToScreen(UnitPoint.Uv(uv), out var p0))
            {
                result = 0;
                return false;
            }

            if (!TryToScreen(UnitPoint.Uv(uv + new Vector2(0, uvSize)), out var p1))
            {
                result = 0;
                return false;
            }

            var dx = p1.X - p0.X;
            var dy = p1.Y - p0.Y;

            result = MathF.Sqrt(dx * dx + dy * dy);
            return true;
        }

        protected bool TryToScreenSize(UnitValue value, Vector3 point, out float result)
        {
            Debug.Assert(_camera != null);

            switch (value.Unit)
            {
                case DrawUnit.Pixel:
                    result = value.Value;
                    return true;

                case DrawUnit.Uv:
                    result = value.Value * _camera.ViewSize.Height;
                    return true;

                case DrawUnit.WorldY:
                    {
                        if (!Transform3.IsIdentity)
                            point = point.Transform(Transform3);

                        if (!TryToScreen(point, false, out var p0))
                        {
                            result = 0;
                            return false;
                        }

                        if (!TryToScreen(point + new Vector3(0, value.Value, 0), false, out var p1))
                        {
                            result = 0;
                            return false;
                        }

                        var dx = p1.X - p0.X;
                        var dy = p1.Y - p0.Y;

                        result = MathF.Sqrt(dx * dx + dy * dy);
                        return true;
                    }

                case DrawUnit.LocalY:
                    {
                        if (!TryToScreen(point, true, out var p0))
                        {
                            result = 0;
                            return false;
                        }

                        if (!TryToScreen(point + new Vector3(0, value.Value, 0), true, out var p1))
                        {
                            result = 0;
                            return false;
                        }

                        var dx = p1.X - p0.X;
                        var dy = p1.Y - p0.Y;

                        result = MathF.Sqrt(dx * dx + dy * dy);
                        return true;
                    }

                default:
                    result = 0;
                    return false;
            }
        }

        protected SKFont GetFont(float size)
        {
            if (!_fonts.TryGetValue(size, out var font))
            {
                var type = string.IsNullOrEmpty(FontFamily)
                    ? SKTypeface.Default
                    : SKTypeface.FromFamilyName(FontFamily) ?? SKTypeface.Default;

                font = new SKFont(type, size);
                _fonts[size] = font;
            }

            return font;
        }

        protected SKColor ToSkColor(Color color)
        {
            return new SKColor(
                (byte)(color.R * 255),
                (byte)(color.G * 255),
                (byte)(color.B * 255),
                (byte)(color.A * 255));
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

        protected SKPoint ToTextPoint(SKPoint point, SKFont font, Align vAlign)
        {
            var metrics = font.Metrics;

            var y = vAlign switch
            {
                Align.Start => point.Y - metrics.Ascent,
                Align.Center => point.Y - (metrics.Ascent + metrics.Descent) * 0.5f,
                Align.End => point.Y - metrics.Descent,
                _ => point.Y
            };

            return new SKPoint(point.X, y);
        }

        public void DrawText(string text, UnitPoint point, UnitValue size, Color color, Align alignX = Align.Center, Align alignY = Align.Center)
        {
            if (!TryToScreen(point, out var screenPoint))
                return;

            if (!TryToScreenSize(size, point, out var fontSize))
                return;

            if (fontSize <= 0 || float.IsNaN(fontSize))
                return;

            var font = GetFont(fontSize);

            _canvas!.DrawText(
                text,
                ToTextPoint(screenPoint, font, alignY),
                (SKTextAlign)alignX,
                font,
                GetFill(color));
        }

        public void DrawText(string text, Vector3 point, UnitValue size, Color color, Align alignX = Align.Center, Align alignY = Align.Center)
        {
            if (!TryToScreen(point, out var screenPoint))
                return;

            if (!TryToScreenSize(size, point, out var fontSize))
                return;

            if (fontSize <= 0 || float.IsNaN(fontSize))
                return;

            var font = GetFont(fontSize);

            _canvas!.DrawText(
                text,
                ToTextPoint(screenPoint, font, alignY),
                (SKTextAlign)alignX,
                font,
                GetFill(color));
        }

        public void DrawTriangle(Triangle3 triangle, Color fillColor, Color strokeColor, UnitValue strokeSize)
        {
            if (!TryToScreen(triangle.V0, out var p0))
                return;

            if (!TryToScreen(triangle.V1, out var p1))
                return;

            if (!TryToScreen(triangle.V2, out var p2))
                return;

            using var builder = new SKPathBuilder();

            builder.AddPoly([p0, p1, p2], true);

            var path = builder.Snapshot();

            _canvas!.DrawPath(path, GetFill(fillColor));

            if (!TryToScreenSize(strokeSize, triangle.V0, out var stroke))
                return;

            if (stroke > 0 && !float.IsNaN(stroke))
                _canvas.DrawPath(path, GetStroke(strokeColor, stroke));
        }

        public void DrawLine(UnitPoint from, UnitPoint to, Color strokeColor, UnitValue strokeSize)
        {
            if (!TryToScreen(from, out var p0))
                return;

            if (!TryToScreen(to, out var p1))
                return;

            if (!TryToScreenSize(strokeSize, from, out var stroke))
                return;

            if (stroke <= 0 || float.IsNaN(stroke))
                return;

            _canvas!.DrawLine(p0, p1, GetStroke(strokeColor, stroke));
        }

        public void DrawLine(Line3 line, Color strokeColor, UnitValue strokeSize)
        {
            if (!TryToScreen(line.From, out var p0))
                return;

            if (!TryToScreen(line.To, out var p1))
                return;

            if (!TryToScreenSize(strokeSize, line.From, out var stroke))
                return;

            if (stroke <= 0 || float.IsNaN(stroke))
                return;

            _canvas!.DrawLine(p0, p1, GetStroke(strokeColor, stroke));
        }

        public void DrawRect(float x, float y, float width, float height, Color fillColor, Color strokeColor, UnitValue strokeSize, DrawUnit unit = DrawUnit.Uv)
        {
            DrawRect(
                new UnitPoint(x, y, unit),
                 new UnitPoint(width, height, unit),
                fillColor,
                strokeColor,
                strokeSize);
        }

        public void DrawRect(UnitPoint point, UnitPoint size, Color fillColor, Color strokeColor, UnitValue strokeSize)
        {
            if (!TryToUvNoPad(point, out var uv0))
                return;

            if (!TryToUvNoPad(size, out var uvSize))
                return;

            if (!TryToScreen(UnitPoint.Uv(uv0), out var p0))
                return;

            if (!TryToScreen(UnitPoint.Uv(uv0 + uvSize), out var p1))
                return;

            var rect = new SKRect(
                MathF.Min(p0.X, p1.X),
                MathF.Min(p0.Y, p1.Y),
                MathF.Max(p0.X, p1.X),
                MathF.Max(p0.Y, p1.Y));

            _canvas!.DrawRect(rect, GetFill(fillColor));

            if (!TryToScreenSize(strokeSize, UnitPoint.Uv(uv0), out var stroke))
                return;

            if (stroke > 0 && !float.IsNaN(stroke))
                _canvas.DrawRect(rect, GetStroke(strokeColor, stroke));
        }

        public void Configure(SKCanvas canvas, Camera camera, int activeEye, Size2I size)
        {
            _canvas = canvas;
            _camera = camera;
            _size = size;
            _activeEye = activeEye;

            var viewSize = camera.ViewSize;

            _scaleX = (float)_size.Width / viewSize.Width;
            _scaleY = (float)_size.Height / viewSize.Height;

            canvas.Scale(_scaleX, _scaleY);

            UpdateView();
        }

        protected void UpdateView()
        {
            _hasUiPlane = false;

            if (_camera == null)
                return;

            var distance = _distance == 0 ? _camera!.Near : _distance;

            if (distance <= 0)
                return;

            var planePoint = _camera.WorldPosition + _camera.Forward * distance;
            var planeNormal = _camera.Forward;

            bool BuildPoint(Vector2 uv, out Vector3 point)
            {
                var ndcX = uv.X * 2.0f - 1.0f;
                var ndcY = 1.0f - uv.Y * 2.0f;

                var nearClip = new Vector4(ndcX, ndcY, 0.0f, 1.0f);
                var farClip = new Vector4(ndcX, ndcY, 1.0f, 1.0f);

                point = Vector3.Zero;

                var count = 0;
                var eyeCount = _camera.Eyes != null && _camera.Eyes.Length > 1
                    ? _camera.Eyes.Length
                    : 1;

                for (var i = 0; i < eyeCount; i++)
                {
                    var invViewProj = _camera.Eyes != null && _camera.Eyes.Length > 1
                        ? _camera.Eyes[i].ViewProjInv
                        : _camera.ViewProjectionInverse;

                    var nearWorld4 = Vector4.Transform(nearClip, invViewProj);
                    var farWorld4 = Vector4.Transform(farClip, invViewProj);

                    if (MathF.Abs(nearWorld4.W) <= 1e-8f || MathF.Abs(farWorld4.W) <= 1e-8f)
                        continue;

                    var nearWorld = new Vector3(
                        nearWorld4.X / nearWorld4.W,
                        nearWorld4.Y / nearWorld4.W,
                        nearWorld4.Z / nearWorld4.W);

                    var farWorld = new Vector3(
                        farWorld4.X / farWorld4.W,
                        farWorld4.Y / farWorld4.W,
                        farWorld4.Z / farWorld4.W);

                    var rayDir = Vector3.Normalize(farWorld - nearWorld);
                    var denom = Vector3.Dot(rayDir, planeNormal);

                    if (MathF.Abs(denom) <= 1e-8f)
                        continue;

                    var t = Vector3.Dot(planePoint - nearWorld, planeNormal) / denom;

                    if (t < -1e-5f)
                        continue;

                    if (t < 0)
                        t = 0;

                    point += nearWorld + rayDir * t;
                    count++;
                }

                if (count == 0)
                    return false;

                point /= count;
                return true;
            }

            if (!BuildPoint(new Vector2(0, 0), out var p00))
                return;

            if (!BuildPoint(new Vector2(1, 0), out var p10))
                return;

            if (!BuildPoint(new Vector2(0, 1), out var p01))
                return;

            _uiOrigin = p00;
            _uiAxisX = p10 - p00;
            _uiAxisY = p01 - p00;
            _hasUiPlane = true;
        }

        public string? FontFamily { get; set; }

        public Matrix4x4 Transform3 { get; set; }

        public Vector2 Padding { get; set; }

        public float Distance
        {
            get => _distance;
            set
            {
                if (_distance == value)
                    return;
                _distance = value;
                UpdateView();
            }
        }


        public SKCanvas? Canvas => _canvas;

        public Camera? Camera => _camera;

        public Size2I Size => _size;
    }
}
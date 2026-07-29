using SkiaSharp;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKPoint ToScreen(UnitPoint point)
        {
            if (!TryToScreen(point, out var result))
                return new SKPoint(float.NaN, float.NaN);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SKPoint ToScreen(Vector3 point)
        {
            if (!TryToScreen(point, out var result))
                return new SKPoint(float.NaN, float.NaN);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToScreenSize(UnitValue value, UnitPoint point)
        {
            if (!TryToScreenSize(value, point, out var result))
                return float.NaN;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ToScreenSize(UnitValue value, Vector3 point)
        {
            if (!TryToScreenSize(value, point, out var result))
                return float.NaN;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 ToUvNoPad(UnitPoint value)
        {
            TryToUvNoPad(value, out var result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool TryToScreen(Vector3 point, out SKPoint result)
        {
            return TryToScreen(point, true, out result);
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

        protected static SKColor ToSkColor(Color color)
        {
            return new SKColor(
                (byte)(color.R * 255),
                (byte)(color.G * 255),
                (byte)(color.B * 255),
                (byte)(color.A * 255));
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

        protected static SKPoint ToTextPoint(SKPoint point, SKFont font, Align vAlign)
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

        public void DrawCube(Vector3 center, Vector3 size, Color strokeColor, UnitValue strokeSize)
        {
            var half = size * 0.5f;

            var x0 = center.X - half.X;
            var x1 = center.X + half.X;
            var y0 = center.Y - half.Y;
            var y1 = center.Y + half.Y;
            var z0 = center.Z - half.Z;
            var z1 = center.Z + half.Z;

            var p000 = new Vector3(x0, y0, z0);
            var p100 = new Vector3(x1, y0, z0);
            var p110 = new Vector3(x1, y1, z0);
            var p010 = new Vector3(x0, y1, z0);

            var p001 = new Vector3(x0, y0, z1);
            var p101 = new Vector3(x1, y0, z1);
            var p111 = new Vector3(x1, y1, z1);
            var p011 = new Vector3(x0, y1, z1);

            DrawLine(p000, p100, strokeColor, strokeSize);
            DrawLine(p100, p110, strokeColor, strokeSize);
            DrawLine(p110, p010, strokeColor, strokeSize);
            DrawLine(p010, p000, strokeColor, strokeSize);

            DrawLine(p001, p101, strokeColor, strokeSize);
            DrawLine(p101, p111, strokeColor, strokeSize);
            DrawLine(p111, p011, strokeColor, strokeSize);
            DrawLine(p011, p001, strokeColor, strokeSize);

            DrawLine(p000, p001, strokeColor, strokeSize);
            DrawLine(p100, p101, strokeColor, strokeSize);
            DrawLine(p110, p111, strokeColor, strokeSize);
            DrawLine(p010, p011, strokeColor, strokeSize);
        }

        public void DrawRect(
            Vector3 topLeft,
            Vector3 topRight,
            Vector3 bottomRight,
            Vector3 bottomLeft,
            Color fillColor,
            Color strokeColor,
            UnitValue strokeSize)
        {
            if (!TryToScreen(topLeft, out var p0))
                return;

            if (!TryToScreen(topRight, out var p1))
                return;

            if (!TryToScreen(bottomRight, out var p2))
                return;

            if (!TryToScreen(bottomLeft, out var p3))
                return;

            using var builder = new SKPathBuilder();

            builder.AddPoly([p0, p1, p2, p3], true);

            var path = builder.Snapshot();

            _canvas!.DrawPath(path, GetFill(fillColor));

            if (!TryToScreenSize(strokeSize, topLeft, out var stroke))
                return;

            if (stroke > 0 && !float.IsNaN(stroke))
                _canvas.DrawPath(path, GetStroke(strokeColor, stroke));
        }

        public void DrawLine(Vector3 from, Vector3 to, Color strokeColor, UnitValue strokeSize)
        {
            DrawLine(new Line3(from, to), strokeColor, strokeSize);
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

            var distance = _distance == 0 ? _camera.Near : _distance;

            if (distance <= 0)
                return;

            var center = _camera.WorldPosition + _camera.Forward * distance;
            var right = _camera.Right;
            var up = _camera.Up;

            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;

            void AddProjection(Matrix4x4 proj, Vector3 eyeWorldPosition)
            {
                var localEye = eyeWorldPosition - _camera.WorldPosition;

                var eyeX = Vector3.Dot(localEye, right);
                var eyeY = Vector3.Dot(localEye, up);

                var halfW = distance / proj.M11;
                var halfH = distance / proj.M22;

                minX = MathF.Min(minX, eyeX - halfW);
                maxX = MathF.Max(maxX, eyeX + halfW);

                minY = MathF.Min(minY, eyeY - halfH);
                maxY = MathF.Max(maxY, eyeY + halfH);
            }

            if (_camera.Eyes != null && _camera.Eyes.Length > 1)
            {
                for (var i = 0; i < _camera.Eyes.Length; i++)
                {
                    var eye = _camera.Eyes[i];

                    AddProjection(
                        eye.Projection,
                        eye.World.Translation);
                }
            }
            else
            {
                AddProjection(
                    _camera.Projection,
                    _camera.WorldPosition);
            }

            _uiOrigin = center + right * minX + up * maxY;
            _uiAxisX = right * (maxX - minX);
            _uiAxisY = up * (minY - maxY);
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
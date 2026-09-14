using System.Numerics;

namespace XrEngine
{
    public class ColorLut
    {
        readonly int _resolution;
        readonly List<Func<Vector3, Vector3>> _transforms = [];

        public ColorLut(int resolution)
        {
            if (resolution < 2)
                throw new ArgumentOutOfRangeException(nameof(resolution));

            _resolution = resolution;
        }

        public ColorLut Transform(Func<Vector3, Vector3> transform)
        {
            ArgumentNullException.ThrowIfNull(transform);

            _transforms.Add(transform);
            return this;
        }

        public ColorLut Exposure(float stops)
        {
            var scale = MathF.Pow(2, stops);

            return Transform(c => c * scale);
        }

        public ColorLut Brightness(float value)
        {
            return Transform(c => c + new Vector3(value));
        }

        public ColorLut Contrast(float value)
        {
            return Transform(c => (c - new Vector3(0.5f)) * value + new Vector3(0.5f));
        }

        public ColorLut Saturation(float value)
        {
            return Transform(c =>
            {
                var luma = Vector3.Dot(c, new Vector3(0.2126f, 0.7152f, 0.0722f));
                return Vector3.Lerp(new Vector3(luma), c, value);
            });
        }

        public ColorLut Hue(float radians)
        {
            var cos = MathF.Cos(radians);
            var sin = MathF.Sin(radians);

            return Transform(c =>
            {
                var y = 0.299f * c.X + 0.587f * c.Y + 0.114f * c.Z;
                var i = 0.596f * c.X - 0.274f * c.Y - 0.322f * c.Z;
                var q = 0.211f * c.X - 0.523f * c.Y + 0.312f * c.Z;

                var ri = i * cos - q * sin;
                var rq = i * sin + q * cos;

                return new Vector3(
                    y + 0.956f * ri + 0.621f * rq,
                    y - 0.272f * ri - 0.647f * rq,
                    y - 1.106f * ri + 1.703f * rq);
            });
        }

        public ColorLut HueDegrees(float degrees)
        {
            return Hue(degrees * MathF.PI / 180f);
        }

        public ColorLut Lift(Vector3 value)
        {
            return Transform(c => c + value * (Vector3.One - c));
        }

        public ColorLut Lift(float value)
        {
            return Lift(new Vector3(value));
        }

        public ColorLut Gain(Vector3 value)
        {
            return Transform(c => c * value);
        }

        public ColorLut Gain(float value)
        {
            return Gain(new Vector3(value));
        }

        public ColorLut Gamma(Vector3 value)
        {
            if (value.X == 0 || value.Y == 0 || value.Z == 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            return Transform(c => new Vector3(
                MathF.Pow(MathF.Max(c.X, 0), 1 / value.X),
                MathF.Pow(MathF.Max(c.Y, 0), 1 / value.Y),
                MathF.Pow(MathF.Max(c.Z, 0), 1 / value.Z)));
        }

        public ColorLut Gamma(float value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            return Gamma(new Vector3(value));
        }

        public ColorLut Temperature(float value)
        {
            return Transform(c =>
            {
                var gain = new Vector3(
                    1 + value * 0.15f,
                    1,
                    1 - value * 0.15f);

                return c * gain;
            });
        }

        public ColorLut Tint(float value)
        {
            return Transform(c =>
            {
                var gain = new Vector3(
                    1 + value * 0.075f,
                    1 - value * 0.15f,
                    1 + value * 0.075f);

                return c * gain;
            });
        }

        public ColorLut LiftGammaGain(Vector3 lift, Vector3 gamma, Vector3 gain)
        {
            return Lift(lift).Gamma(gamma).Gain(gain);
        }

        public ColorLut TealOrange(float amount = 1f)
        {
            return Transform(c =>
            {
                var warm = new Vector3(
                    c.X * 1.08f + c.Y * 0.02f,
                    c.Y * 0.98f,
                    c.Z * 0.90f);

                var cool = new Vector3(
                    c.X * 0.92f,
                    c.Y * 1.02f,
                    c.Z * 1.10f);

                var luma = Vector3.Dot(c, new Vector3(0.2126f, 0.7152f, 0.0722f));
                var target = Vector3.Lerp(cool, warm, luma);

                return Vector3.Lerp(c, target, amount);
            });
        }

        public ColorLut BleachBypass(float amount = 1f)
        {
            return Transform(c =>
            {
                var luma = Vector3.Dot(c, new Vector3(0.2126f, 0.7152f, 0.0722f));
                var gray = new Vector3(luma);
                var contrast = (gray - new Vector3(0.5f)) * 1.35f + new Vector3(0.5f);
                var result = Vector3.Lerp(c, contrast, 0.65f);

                return Vector3.Lerp(c, result, amount);
            });
        }

        public ColorLut WarmFilm(float amount = 1f)
        {
            return Transform(c =>
            {
                var result = new Vector3(
                    c.X * 1.06f,
                    c.Y * 1.01f,
                    c.Z * 0.94f);

                result = new Vector3(
                    MathF.Pow(MathF.Max(result.X, 0), 0.96f),
                    MathF.Pow(MathF.Max(result.Y, 0), 0.98f),
                    MathF.Pow(MathF.Max(result.Z, 0), 1.03f));

                return Vector3.Lerp(c, result, amount);
            });
        }

        public ColorLut FadedFilm(float amount = 1f)
        {
            return Transform(c =>
            {
                var result = new Vector3(
                    MathF.Pow(MathF.Max(c.X, 0), 0.92f),
                    MathF.Pow(MathF.Max(c.Y, 0), 0.96f),
                    MathF.Pow(MathF.Max(c.Z, 0), 1.04f));

                result = result * 0.92f + new Vector3(0.04f, 0.035f, 0.03f);

                return Vector3.Lerp(c, result, amount);
            });
        }

        public ColorLut CoolCinema(float amount = 1f)
        {
            return Transform(c =>
            {
                var result = new Vector3(
                    c.X * 0.94f,
                    c.Y,
                    c.Z * 1.08f);

                result = (result - new Vector3(0.5f)) * 1.12f + new Vector3(0.5f);

                return Vector3.Lerp(c, result, amount);
            });
        }

        public ColorLut Grayscale(float amount = 1f)
        {
            return Transform(c =>
            {
                var luma = Vector3.Dot(c, new Vector3(0.2126f, 0.7152f, 0.0722f));
                return Vector3.Lerp(c, new Vector3(luma), amount);
            });
        }

        public ColorLut Invert(float amount = 1f)
        {
            return Transform(c => Vector3.Lerp(c, Vector3.One - c, amount));
        }

        public ColorLut Sepia(float amount = 1f)
        {
            return Transform(c =>
            {
                var result = new Vector3(
                    c.X * 0.393f + c.Y * 0.769f + c.Z * 0.189f,
                    c.X * 0.349f + c.Y * 0.686f + c.Z * 0.168f,
                    c.X * 0.272f + c.Y * 0.534f + c.Z * 0.131f);

                return Vector3.Lerp(c, result, amount);
            });
        }

        public ColorLut ColorMatrix(Matrix4x4 matrix)
        {
            return Transform(c =>
            {
                var result = Vector4.Transform(new Vector4(c, 1), matrix);
                return new Vector3(result.X, result.Y, result.Z);
            });
        }

        public Vector3 TransformColor(Vector3 color)
        {
            foreach (var transform in _transforms)
                color = transform(color);

            return Vector3.Clamp(color, Vector3.Zero, Vector3.One);
        }

        public byte[] BuildData()
        {
            var data = new byte[_resolution * _resolution * _resolution * 3];
            var scale = 1f / (_resolution - 1);
            var p = 0;

            for (var b = 0; b < _resolution; b++)
                for (var g = 0; g < _resolution; g++)
                    for (var r = 0; r < _resolution; r++)
                    {
                        var color = TransformColor(new Vector3(r * scale, g * scale, b * scale));

                        data[p++] = ToByte(color.X);
                        data[p++] = ToByte(color.Y);
                        data[p++] = ToByte(color.Z);
                    }

            return data;
        }

        public ColorLut Clear()
        {
            _transforms.Clear();
            return this;
        }

        private static byte ToByte(float value)
        {
            return (byte)MathF.Round(Math.Clamp(value, 0, 1) * 255);
        }

        public int Resolution => _resolution;

        public int TransformCount => _transforms.Count;
    }
}
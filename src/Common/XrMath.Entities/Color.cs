using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace XrMath
{
    public struct Color
    {
        public Color()
        {
        }

        public Color(float[] array)
        {
            R = array[0];
            G = array[1];
            B = array[2];

            if (array.Length == 4)
                A = array[3];
        }

        public Color(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Color Rgb(float value)
        {
            return new Color()
            {
                R = value,
                G = value,
                B = value,
                A = 1f
            };
        }

        public static explicit operator Vector3(Color color)
        {
            return new Vector3(color.R, color.G, color.B);
        }

        public static implicit operator Vector4(Color color)
        {
            return new Vector4(color.R, color.G, color.B, color.A);
        }

        public static implicit operator Color(string color)
        {
            return Parse(color);
        }
        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color left, Color right)
        {
            return !(left == right);
        }

        public static Color Parse(string colorString)
        {
            colorString = colorString.TrimStart('#');

            if (colorString.Length == 3)
            {
                colorString = $"{colorString[0]}{colorString[0]}{colorString[1]}{colorString[1]}{colorString[2]}{colorString[2]}";
            }

            int r = Convert.ToInt32(colorString.Substring(0, 2), 16);
            int g = Convert.ToInt32(colorString.Substring(2, 2), 16);
            int b = Convert.ToInt32(colorString.Substring(4, 2), 16);
            int a = colorString.Length == 8 ? Convert.ToInt32(colorString.Substring(6, 2), 16) : 255;

            float rf = r / 255f;
            float gf = g / 255f;
            float bf = b / 255f;
            float af = a / 255f;

            return new Color(rf, gf, bf, af);
        }

        public readonly override string ToString()
        {
            return $"<{R},{G},{B},{A}>";
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Color color)
                return R == color.R && G == color.G && B == color.B && A == color.A;
            return false;
        }

        public readonly override int GetHashCode()
        {
            return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();
        }

        public float[] ToArray()
        {
            return [R, G, B, A];
        }

        public unsafe void ToBytes(byte* dst)
        {
            dst[0] = (byte)(R * 255);
            dst[1] = (byte)(G * 255);
            dst[2] = (byte)(B * 255);
            dst[3] = (byte)(A * 255);
        }

        public readonly Color ToSrgb()
        {
            static float LinearToSrgb(float c)
            {
                if (c <= 0.0031308f)
                    return 12.92f * c;
           
                return 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
            }

            return new Color(LinearToSrgb(R), LinearToSrgb(G), LinearToSrgb(B), A);
        }

        public readonly Color ToLinear()
        {
            static float SrgbToLinear(float c)
            {
                if (c <= 0.04045f)
                    return c / 12.92f;
                else
                    return MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
            }

            return new Color(SrgbToLinear(R), SrgbToLinear(G), SrgbToLinear(B), A);
        }


        public static Color operator *(Color a, float v)
        {
            return new Color(a.R * v, a.G * v, a.B * v, a.A * v);
        }

        public float R;

        public float G;

        public float B;

        public float A;

        public static Color Black => new(0f, 0f, 0f);

        public static Color White => new(1f, 1f, 1f);

        public static Color Transparent => new(0f, 0f, 0f, 0f);


    }
}

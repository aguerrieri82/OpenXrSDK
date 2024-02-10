using System.Numerics;

namespace OpenXr.Engine
{
    public struct Color
    {
        public Color()
        {
        }

        public Color(float r, float g, float b, float a = 1f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public void Rgb(float value)
        {
            R = value;
            G = value;
            B = value;
        }

        public static implicit operator Vector3(Color color)
        {
            return new Vector3(color.R, color.G, color.B);
        }

        public static Color White => new Color(1f, 1f, 1f, 1f);

        public float R;

        public float G;

        public float B;

        public float A;
    }
}

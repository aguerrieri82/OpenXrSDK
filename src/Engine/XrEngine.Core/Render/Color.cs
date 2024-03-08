using System.Numerics;

namespace XrEngine
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
            A = array[3];
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

        public static explicit operator Vector3(Color color)
        {
            return new Vector3(color.R, color.G, color.B);
        }

        public static implicit operator Vector4(Color color)
        {
            return new Vector4(color.R, color.G, color.B, color.A);
        }

        public override string ToString()
        {
            return $"<{R},{G},{B},{A}>";
        }

        public static Color White => new Color(1f, 1f, 1f, 1f);

        public static Color Transparent => new Color(0f, 0f, 0f, 0f);


        public float R;

        public float G;

        public float B;

        public float A;
    }
}

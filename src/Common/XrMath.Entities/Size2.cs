using System.Diagnostics.CodeAnalysis;

namespace XrMath
{
    public struct Size2 : IEquatable<Size2> 
    {
        public Size2() { }
       
        public Size2(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public static Size2 operator -(Size2 a, Size2 b)
        {
            return new Size2 { Width = a.Width - b.Width, Height = a.Height - b.Height };
        }

        public static Size2 operator +(Size2 a, Size2 b)
        {
            return new Size2 { Width = a.Width + b.Width, Height = a.Height + b.Height };
        }

        public static bool operator !=(Size2 a, Size2 b)
        {
            return !Equals(a, b);
        }

        public static bool operator ==(Size2 a, Size2 b)
        {
            return Equals(a, b);
        }

        public readonly bool Equals(Size2 other)
        {
            return other.Width == Width && other.Height == Height;  
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Size2 other)
                return Equals(other);
            return false;
        }

        public readonly override int GetHashCode()
        {
            return (int)(Width * Height);
        }

        public static Size2 Zero => new(0, 0);


        public float Width;

        public float Height;

    }
}
